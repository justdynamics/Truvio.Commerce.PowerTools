using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.Features.PimQuality.Core;
using Truvio.Commerce.PowerTools.Features.PimQuality.Models;
using Truvio.Commerce.PowerTools.Features.Settings.Core;
using Truvio.Commerce.PowerTools.Features.Settings.Dw;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Queries;

/// <summary>One product family in full: score, per-rule fields, per-language layers, variants.</summary>
public sealed class PimProductQualityQuery : DataQueryModelBase<PimProductQualityModel>
{
    public string ProductId { get; set; } = string.Empty;

    public string LanguageId { get; set; } = string.Empty;

    /// <summary>Carried so "back to the explorer" keeps the scope the user came from.</summary>
    public string GroupId { get; set; } = string.Empty;

    public override PimProductQualityModel? GetModel()
    {
        if (string.IsNullOrEmpty(ProductId))
            return new PimProductQualityModel { Title = "Product quality", Error = "No product selected" };

        try
        {
            var source = PimQueryHelpers.Source();
            var product = source.GetProductDetail(ProductId, LanguageId);
            if (product is null)
            {
                return new PimProductQualityModel
                {
                    Title = ProductId,
                    Error = $"Product '{ProductId}' was not found in language '{(string.IsNullOrEmpty(LanguageId) ? "(default)" : LanguageId)}'."
                };
            }

            var settings = DwPowerToolsSettings.Current;
            var threshold = PowerToolsSettings.Positive(settings.PimCompletenessThreshold, PimQualityEngine.DefaultThreshold);
            var rules = source.GetRules();
            var sections = new List<PimSectionModel>();

            // ---- Missing fields ---------------------------------------------------------------
            var missing = new PimSectionModel { Heading = "Missing fields" };
            if (product.MissingFields.Count == 0)
            {
                missing.Rows.Add(PimQueryHelpers.Row("Fields", "Complete", "win", string.Empty,
                    "Every field the applicable completion rules require has a value in this language."));
            }
            else
            {
                foreach (var field in product.MissingFields)
                {
                    var owner = rules.FirstOrDefault(r => r.FieldSystemNames.Contains(field, StringComparer.OrdinalIgnoreCase));
                    missing.Rows.Add(PimQueryHelpers.Row(field, "Missing", "reject", string.Empty,
                        owner is null
                            ? "Required by a completion rule in effect for this product."
                            : $"Required by rule '{owner.Name}'."));
                }
            }
            sections.Add(missing);

            // ---- Language layers --------------------------------------------------------------
            if (product.ScorePerLanguage.Count > 1)
            {
                var languages = new PimSectionModel { Heading = "Language layers" };
                var best = product.ScorePerLanguage.Values.Max();
                foreach (var pair in product.ScorePerLanguage.OrderByDescending(p => p.Value))
                {
                    var behind = best - pair.Value;
                    languages.Rows.Add(PimQueryHelpers.Row(
                        pair.Key,
                        PimQueryHelpers.Percent(pair.Value),
                        PimQueryHelpers.ScoreKind(pair.Value, threshold),
                        string.Empty,
                        behind == 0 ? "The most complete layer for this product." : $"{behind} points behind the best layer."));
                }
                sections.Add(languages);
            }

            // ---- Variants ----------------------------------------------------------------------
            if (product.Variants.Count > 0)
            {
                var variants = new PimSectionModel { Heading = $"Variants ({product.Variants.Count})" };
                foreach (var variant in product.Variants.OrderBy(v => v.Score))
                {
                    variants.Rows.Add(PimQueryHelpers.Row(
                        variant.VariantId,
                        PimQueryHelpers.Percent(variant.Score),
                        PimQueryHelpers.ScoreKind(variant.Score, threshold),
                        variant.DisplayName,
                        variant.MissingFields.Count == 0
                            ? "Complete."
                            : $"Missing: {string.Join(", ", variant.MissingFields.Take(5))}"));
                }
                sections.Add(variants);
            }

            // ---- Rules in effect ----------------------------------------------------------------
            var ruleSection = new PimSectionModel { Heading = "Completion rules" };
            if (rules.Count == 0)
            {
                ruleSection.Rows.Add(PimQueryHelpers.Row("Rules", "none", string.Empty, string.Empty,
                    "No completion rules are defined, so every product scores against nothing."));
            }
            else
            {
                foreach (var rule in rules)
                {
                    ruleSection.Rows.Add(PimQueryHelpers.Row(
                        rule.Name,
                        rule.IsDead ? "Dead" : "Assigned",
                        rule.IsDead ? "warn" : "info",
                        string.Join(", ", rule.FieldSystemNames.Take(6)),
                        rule.IsDead
                            ? "Assigned to no shop, group or query — it scores nothing."
                            : $"Applies via {string.Join("; ", rule.Usages.Take(3))}" +
                              (rule.ExcludeVariants ? ". Variants excluded." : string.Empty)));
                }
            }
            sections.Add(ruleSection);

            var behindCount = product.ScorePerLanguage.Count <= 1
                ? 0
                : product.ScorePerLanguage.Count(p => product.ScorePerLanguage.Values.Max() - p.Value >= 10);

            return new PimProductQualityModel
            {
                Title = product.DisplayName,
                ProductId = product.ProductId,
                LanguageId = product.LanguageId,
                ProductName = $"{product.Number} - {product.DisplayName}".TrimStart(' ', '-'),
                Score = PimQueryHelpers.Percent(product.Score),
                ScoreValue = product.Score,
                RulesApplied = PimQueryHelpers.Count(rules.Count(r => !r.IsDead)),
                MissingCount = PimQueryHelpers.Count(product.MissingFields.Count),
                LanguagesBehind = behindCount == 0 ? "none" : PimQueryHelpers.Count(behindCount),
                Sections = sections
            };
        }
        catch (Exception ex)
        {
            return new PimProductQualityModel { Title = ProductId, Error = ex.Message };
        }
    }
}
