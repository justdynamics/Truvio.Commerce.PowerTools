using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Core;

/// <summary>The headline numbers shown on the Catalog quality screen.</summary>
public sealed record PimQuality(
    int ProductsScanned,
    int TotalProductCount,
    int AverageScore,
    int BelowThresholdCount,
    int VariantGapCount,
    int BrokenImageCount,
    int DeadRuleCount,
    IReadOnlyList<(string Field, int Count)> WorstFields,
    IReadOnlyList<Finding> Findings)
{
    public int CriticalCount => Findings.Count(f => f.Severity == FindingSeverity.Critical);

    public int WarningCount => Findings.Count(f => f.Severity == FindingSeverity.Warning);

    /// <summary>One word for the whole catalog, driven by the worst finding present.</summary>
    public string Verdict =>
        CriticalCount > 0 ? "Attention needed"
        : WarningCount > 0 ? "Needs a look"
        : "Healthy";

    public bool Healthy => CriticalCount == 0 && WarningCount == 0;

    /// <summary>The one field worth fixing first, or empty when nothing stands out.</summary>
    public string WorstField => WorstFields.Count == 0 ? string.Empty : WorstFields[0].Field;
}
