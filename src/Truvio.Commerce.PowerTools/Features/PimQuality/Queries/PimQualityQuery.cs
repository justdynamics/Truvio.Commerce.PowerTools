using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.Features.PimQuality.Core;
using Truvio.Commerce.PowerTools.Features.PimQuality.Models;
using Truvio.Commerce.PowerTools.Features.Settings.Core;
using Truvio.Commerce.PowerTools.Features.Settings.Dw;
using Truvio.Commerce.PowerTools.Shared.AdminUI;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Queries;

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
