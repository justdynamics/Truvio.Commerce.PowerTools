namespace Truvio.Commerce.PowerTools.Core.Pim;

/// <summary>
/// Everything the PIM screens read, behind one interface so every rule and every screen model
/// is testable without a DW host. The live implementation
/// (<see cref="Dw.DwPimSource"/>) is the only file in the section that touches Dynamicweb.
/// </summary>
public interface IPimQualitySource
{
    /// <summary>Product groups available as a scope, id and name.</summary>
    IReadOnlyList<(string Id, string Name)> GetGroups();

    /// <summary>Ecom languages, DW's default language first.</summary>
    IReadOnlyList<(string Id, string Name)> GetLanguages();

    /// <summary>
    /// Completeness for the products in scope, capped by <see cref="PimScope.EffectiveCap"/>.
    /// The bulk scoring call is the expensive part of the section, so this is the one read
    /// every screen shares.
    /// </summary>
    (IReadOnlyList<ProductQuality> Products, int TotalCount) GetProductQuality(PimScope scope);

    /// <summary>One product family in full: master, variants and per-language scores.</summary>
    ProductQuality? GetProductDetail(string productId, string languageId);

    /// <summary>Which rules apply where; empty usages mean dead configuration.</summary>
    IReadOnlyList<RuleUsage> GetRules();

    /// <summary>Variant-combination gaps for the products in scope.</summary>
    IReadOnlyList<VariantGap> GetVariantGaps(PimScope scope);

    /// <summary>Assets attached to the same product more than once.</summary>
    IReadOnlyList<DuplicateAsset> GetDuplicateAssets(PimScope scope);

    /// <summary>Products whose resolved image path has no file behind it.</summary>
    IReadOnlyList<BrokenImage> GetBrokenImages(PimScope scope);

    /// <summary>Product categories with the number of groups using each.</summary>
    IReadOnlyList<CategoryUsage> GetCategories();

    /// <summary>Workflows referenced by groups or products.</summary>
    IReadOnlyList<WorkflowUsage> GetWorkflows();

    /// <summary>
    /// One consistent read for the rules. <paramref name="includeCatalogWide"/> skips the
    /// whole-catalog passes (variant gaps, assets, images) for screens that only need scores.
    /// </summary>
    PimSnapshot Snapshot(PimScope scope, bool includeCatalogWide = true);
}
