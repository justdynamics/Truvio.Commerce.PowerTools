using Truvio.Commerce.PowerTools.Features.PimQuality.Core;

namespace Truvio.Commerce.PowerTools.Tests.Features.PimQuality;

/// <summary>
/// An <see cref="IPimQualitySource"/> that returns exactly what a test hands it — so every
/// screen model and every rule is exercised with no DW host in sight.
/// </summary>
internal sealed class FakePimQualitySource : IPimQualitySource
{
    public List<(string Id, string Name)> Groups { get; init; } = [];

    public List<(string Id, string Name)> Languages { get; init; } = [];

    public List<ProductQuality> Products { get; init; } = [];

    public int TotalCount { get; init; }

    public List<RuleUsage> Rules { get; init; } = [];

    public List<VariantGap> Gaps { get; init; } = [];

    public List<DuplicateAsset> Duplicates { get; init; } = [];

    public List<BrokenImage> BrokenImages { get; init; } = [];

    public List<CategoryUsage> Categories { get; init; } = [];

    public List<WorkflowUsage> Workflows { get; init; } = [];

    /// <summary>Scopes the source was asked for — lets a test assert the cap really travels.</summary>
    public List<PimScope> RequestedScopes { get; } = [];

    public IReadOnlyList<(string Id, string Name)> GetGroups() => Groups;

    public IReadOnlyList<(string Id, string Name)> GetLanguages() => Languages;

    public (IReadOnlyList<ProductQuality> Products, int TotalCount) GetProductQuality(PimScope scope)
    {
        RequestedScopes.Add(scope);
        return (Products, TotalCount == 0 ? Products.Count : TotalCount);
    }

    public ProductQuality? GetProductDetail(string productId, string languageId) =>
        Products.FirstOrDefault(p => p.ProductId == productId);

    public IReadOnlyList<RuleUsage> GetRules() => Rules;

    public IReadOnlyList<VariantGap> GetVariantGaps(PimScope scope) => Gaps;

    public IReadOnlyList<DuplicateAsset> GetDuplicateAssets(PimScope scope) => Duplicates;

    public IReadOnlyList<BrokenImage> GetBrokenImages(PimScope scope) => BrokenImages;

    public IReadOnlyList<CategoryUsage> GetCategories() => Categories;

    public IReadOnlyList<WorkflowUsage> GetWorkflows() => Workflows;

    public PimSnapshot Snapshot(PimScope scope, bool includeCatalogWide = true)
    {
        var (products, total) = GetProductQuality(scope);
        return new PimSnapshot(
            products,
            Rules,
            includeCatalogWide ? Gaps : [],
            includeCatalogWide ? Duplicates : [],
            includeCatalogWide ? BrokenImages : [],
            includeCatalogWide ? Categories : [],
            scope,
            total)
        {
            Workflows = includeCatalogWide ? Workflows : [],
            Languages = Languages.Select(l => l.Id).ToList()
        };
    }
}
