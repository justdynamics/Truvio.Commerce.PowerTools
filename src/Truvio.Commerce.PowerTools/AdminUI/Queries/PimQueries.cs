using System.Globalization;
using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Pim;
using Truvio.Commerce.PowerTools.Core.Pim.Dw;
using Truvio.Commerce.PowerTools.Core.Pim.Rules;
using Truvio.Commerce.PowerTools.Core.Settings;
using Truvio.Commerce.PowerTools.Core.Settings.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>Shared helpers for the PIM-section queries.</summary>
internal static class PimQueryHelpers
{
    public static IPimQualitySource Source() => new DwPimSource();

    /// <summary>The scope every screen shares, with the configured cap applied.</summary>
    public static PimScope Scope(string groupId, string languageId, string? search)
    {
        var settings = DwPowerToolsSettings.Current;
        return new PimScope(
            groupId ?? string.Empty,
            languageId ?? string.Empty,
            PowerToolsSettings.Positive(settings.PimProductCap, PimScope.DefaultProductCap),
            search ?? string.Empty);
    }

    /// <summary>win / warn / reject by score, matching the colour language of the other tools.</summary>
    public static string ScoreKind(int score, int threshold) =>
        score >= 100 ? "win"
        : score >= threshold ? "ok"
        : score == 0 ? "reject"
        : "warn";

    public static string Percent(int value) => $"{value}%";

    public static string Count(int value) => value.ToString("N0", CultureInfo.InvariantCulture);

    public static OpsRowModel Row(string item, string verdict, string kind, string value, string why) => new()
    {
        Item = item,
        Verdict = verdict,
        VerdictKind = kind,
        Value = value,
        Why = why
    };

    /// <summary>The severity badge kind used across the finding tables.</summary>
    public static string SeverityKind(Core.Diagnostics.FindingSeverity severity) => severity switch
    {
        Core.Diagnostics.FindingSeverity.Critical => "reject",
        Core.Diagnostics.FindingSeverity.Warning => "warn",
        _ => "info"
    };
}

/// <summary>
/// The Completeness explorer's rows: DW's completeness for every product family in scope,
/// worst first. Only <see cref="GroupId"/> and <see cref="LanguageId"/> are public — every
/// public property of a query is serialised into the screen URL, so computed values stay
/// methods.
/// </summary>
public sealed class PimCompletenessQuery : DataQueryListBase<PimCompletenessModel, PimCompletenessModel, DataListViewModel<PimCompletenessModel>>
{
    /// <summary>Product group to scan; empty = the whole catalog.</summary>
    public string GroupId { get; set; } = string.Empty;

    /// <summary>Language the scores are read in; empty = DW's default language.</summary>
    public string LanguageId { get; set; } = string.Empty;

    /// <summary>Resolves a toolbar group pick — see <see cref="PickStore"/>.</summary>
    public string GroupPickToken { get; set; } = string.Empty;

    private void ResolvePicks()
    {
        if (!string.IsNullOrEmpty(GroupPickToken) && PickStore.Get(GroupPickToken) is { Length: > 0 } picked)
            GroupId = string.Equals(picked, PimPicks.WholeCatalog, StringComparison.Ordinal) ? string.Empty : picked;
    }

    public PimScope GetScope() => PimQueryHelpers.Scope(GroupId, LanguageId, Search);

    protected override IEnumerable<PimCompletenessModel>? GetListItems()
    {
        ResolvePicks();

        var threshold = PowerToolsSettings.Positive(
            DwPowerToolsSettings.Current.PimCompletenessThreshold, PimQualityEngine.DefaultThreshold);

        var (products, total) = PimQueryHelpers.Source().GetProductQuality(GetScope());

        var items = products.Select(p => new PimCompletenessModel
        {
            ProductId = p.ProductId,
            LanguageId = p.LanguageId,
            ScoreValue = p.Score,
            Number = string.IsNullOrEmpty(p.Number) ? p.ProductId : p.Number,
            Name = p.DisplayName,
            Score = PimQueryHelpers.Percent(p.Score),
            WorstRule = string.IsNullOrEmpty(p.WorstRule) ? "-" : p.WorstRule,
            MissingCount = p.MissingFields.Count.ToString(CultureInfo.InvariantCulture),
            MissingFields = p.MissingFields.Count == 0 ? "-" : string.Join(", ", p.MissingFields.Take(3)) +
                (p.MissingFields.Count > 3 ? $" +{p.MissingFields.Count - 3}" : string.Empty)
        }).ToList();

        // Never truncate silently: the same trailing row the product picker uses.
        if (total > items.Count)
        {
            items.Add(new PimCompletenessModel
            {
                ProductId = string.Empty,
                Number = "...",
                Name = $"{total - items.Count} more products not shown - use the search to narrow the list",
                Score = string.Empty,
                WorstRule = string.Empty,
                MissingCount = string.Empty,
                MissingFields = string.Empty
            });
        }

        _ = threshold;
        return items;
    }

    protected override IEnumerable<PimCompletenessModel> MapModels(IEnumerable<PimCompletenessModel> items) => items;

    protected override DataListViewModel<PimCompletenessModel> MakeListModel() => new();
}

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

/// <summary>The catalog-wide report: rules PIM-W1..W8 plus the "fix this first" ranking.</summary>
public sealed class PimQualityQuery : DataQueryModelBase<PimQualityModel>
{
    public string GroupId { get; set; } = string.Empty;

    public string LanguageId { get; set; } = string.Empty;

    public string GroupPickToken { get; set; } = string.Empty;

    private void ResolvePicks()
    {
        if (!string.IsNullOrEmpty(GroupPickToken) && PickStore.Get(GroupPickToken) is { Length: > 0 } picked)
            GroupId = string.Equals(picked, PimPicks.WholeCatalog, StringComparison.Ordinal) ? string.Empty : picked;
    }

    public PimScope GetScope() => PimQueryHelpers.Scope(GroupId, LanguageId, search: null);

    public override PimQualityModel? GetModel()
    {
        ResolvePicks();

        try
        {
            var settings = DwPowerToolsSettings.Current;
            var snapshot = PimQueryHelpers.Source().Snapshot(GetScope());
            var quality = new PimQualityEngine(settings).Summarise(snapshot);
            var threshold = PowerToolsSettings.Positive(settings.PimCompletenessThreshold, PimQualityEngine.DefaultThreshold);

            // Suppression is never silent — the hidden count is appended as its own row.
            var filtered = settings.FilterPimFindings(quality.Findings);

            var findings = filtered.Visible.Select(f => PimQueryHelpers.Row(
                f.EntityDisplayName,
                f.Severity.ToString(),
                PimQueryHelpers.SeverityKind(f.Severity),
                settings.ShowRuleIds ? $"{f.Title} [{f.RuleId}]" : f.Title,
                f.Detail)).ToList();

            if (filtered.HiddenCount > 0)
            {
                findings.Add(PimQueryHelpers.Row("PowerTools settings", string.Empty, string.Empty,
                    filtered.HiddenNotice(),
                    "Suppressed PIM rule ids are configured under PowerTools > Settings."));
            }

            if (findings.Count == 0)
            {
                findings.Add(PimQueryHelpers.Row("Catalog", "Healthy", "win", string.Empty,
                    $"No rule fired over the {quality.ProductsScanned} product(s) scanned."));
            }

            var worst = quality.WorstFields.Take(10).Select(w => PimQueryHelpers.Row(
                w.Field,
                $"{w.Count} product(s)",
                w.Count * 100 / Math.Max(1, quality.ProductsScanned) >= 50 ? "reject" : "warn",
                string.Empty,
                $"Missing on {w.Count * 100d / Math.Max(1, quality.ProductsScanned):0.#}% of the products scanned.")).ToList();

            if (worst.Count == 0)
            {
                worst.Add(PimQueryHelpers.Row("Fields", "none", string.Empty, string.Empty,
                    "No field is missing on any scanned product."));
            }

            return new PimQualityModel
            {
                Verdict = quality.Verdict,
                Healthy = quality.Healthy,
                ProductsScanned = snapshot.IsTruncated
                    ? $"{PimQueryHelpers.Count(quality.ProductsScanned)} of {PimQueryHelpers.Count(quality.TotalProductCount)}"
                    : PimQueryHelpers.Count(quality.ProductsScanned),
                AverageScore = PimQueryHelpers.Percent(quality.AverageScore),
                BelowThreshold = $"{PimQueryHelpers.Count(quality.BelowThresholdCount)} below {threshold}%",
                VariantGaps = PimQueryHelpers.Count(quality.VariantGapCount),
                BrokenImages = PimQueryHelpers.Count(quality.BrokenImageCount),
                DeadRules = PimQueryHelpers.Count(quality.DeadRuleCount),
                WorstField = string.IsNullOrEmpty(quality.WorstField) ? "-" : quality.WorstField,
                FindingCounts = $"{quality.CriticalCount} critical / {quality.WarningCount} warning",
                WorstFields = worst,
                Findings = findings,
                ScopeNote = snapshot.IsTruncated
                    ? $"Scanned the first {quality.ProductsScanned} of {quality.TotalProductCount} products — raise the product scan cap in PowerTools settings, or narrow the scope with the group picker."
                    : string.Empty
            };
        }
        catch (Exception ex)
        {
            return new PimQualityModel { Error = ex.Message };
        }
    }
}

/// <summary>Completion rules and workflows with their assignments — the governance list.</summary>
public sealed class PimGovernanceQuery : DataQueryListBase<PimGovernanceModel, PimGovernanceModel, DataListViewModel<PimGovernanceModel>>
{
    protected override IEnumerable<PimGovernanceModel>? GetListItems()
    {
        var source = PimQueryHelpers.Source();
        var items = new List<PimGovernanceModel>();

        foreach (var rule in source.GetRules())
        {
            items.Add(new PimGovernanceModel
            {
                State = rule.IsDead ? "Dead" : "Assigned",
                Kind = "Completion rule",
                Name = rule.Name,
                Status = rule.IsDead ? "Dead" : "Assigned",
                AppliesTo = rule.Usages.Count == 0 ? "nothing" : string.Join("; ", rule.Usages.Take(3)) +
                    (rule.Usages.Count > 3 ? $" +{rule.Usages.Count - 3}" : string.Empty),
                Fields = rule.FieldSystemNames.Count == 0 ? "-" : string.Join(", ", rule.FieldSystemNames.Take(4)) +
                    (rule.FieldSystemNames.Count > 4 ? $" +{rule.FieldSystemNames.Count - 4}" : string.Empty)
            });
        }

        foreach (var workflow in source.GetWorkflows())
        {
            var scope = (workflow.UsedByGroups, workflow.UsedByProducts) switch
            {
                (true, true) => "product groups and products",
                (true, false) => "product groups",
                (false, true) => "products",
                _ => "nothing"
            };

            items.Add(new PimGovernanceModel
            {
                State = workflow.IsReferenced ? "Assigned" : "Dead",
                Kind = "Workflow",
                Name = workflow.Name,
                Status = workflow.IsReferenced ? "In use" : "Unused",
                AppliesTo = scope,
                Fields = "-"
            });
        }

        return items
            .Where(i => SearchMatches(i))
            .OrderBy(i => i.Kind, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(i => i.State == "Dead")
            .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private bool SearchMatches(PimGovernanceModel model) =>
        string.IsNullOrWhiteSpace(Search) ||
        new[] { model.Name, model.Kind, model.Status, model.AppliesTo, model.Fields }
            .Any(v => !string.IsNullOrEmpty(v) && v.Contains(Search.Trim(), StringComparison.OrdinalIgnoreCase));

    protected override IEnumerable<PimGovernanceModel> MapModels(IEnumerable<PimGovernanceModel> items) => items;

    protected override DataListViewModel<PimGovernanceModel> MakeListModel() => new();
}

/// <summary>Sentinel ids used by the PIM toolbar pickers.</summary>
internal static class PimPicks
{
    /// <summary>The "no group filter" entry — an empty id cannot round-trip through the picker.</summary>
    public const string WholeCatalog = "__all__";
}
