using Truvio.Commerce.PowerTools.Features.OperationsConsole.Core;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Core;

/// <summary>
/// One consistent read of the catalog that every rule evaluates against. Taking a snapshot
/// first (instead of letting each rule query DW) keeps the rules pure and makes the counts on
/// the overview agree with the rows on the list screens — the same contract
/// <see cref="OperationsSnapshot"/> follows.
/// </summary>
/// <param name="Products">The scanned page only — never assume this is the whole catalog.</param>
/// <param name="TotalProductCount">
/// How many products the scope actually holds. Passed separately because
/// <paramref name="Products"/> is capped: computing "% of products missing field X" against the
/// capped list is honest, against the whole catalog would be a lie.
/// </param>
public sealed record PimSnapshot(
    IReadOnlyList<ProductQuality> Products,
    IReadOnlyList<RuleUsage> Rules,
    IReadOnlyList<VariantGap> VariantGaps,
    IReadOnlyList<DuplicateAsset> DuplicateAssets,
    IReadOnlyList<BrokenImage> BrokenImages,
    IReadOnlyList<CategoryUsage> Categories,
    PimScope Scope,
    int TotalProductCount = 0)
{
    public static PimSnapshot Empty => new([], [], [], [], [], [], PimScope.Default);

    /// <summary>Workflows in use; governance only, so it is not part of the positional contract.</summary>
    public IReadOnlyList<WorkflowUsage> Workflows { get; init; } = [];

    /// <summary>Languages the scan covered, default first.</summary>
    public IReadOnlyList<string> Languages { get; init; } = [];

    /// <summary>True when the catalog holds more products than the cap allowed us to score.</summary>
    public bool IsTruncated => TotalProductCount > Products.Count;

    public int NotShownCount => IsTruncated ? TotalProductCount - Products.Count : 0;

    /// <summary>Mean completeness over the scanned products; 0 when nothing was scanned.</summary>
    public int AverageScore =>
        Products.Count == 0 ? 0 : (int)Math.Round(Products.Average(p => p.Score));
}
