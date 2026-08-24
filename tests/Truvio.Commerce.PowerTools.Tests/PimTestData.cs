using Truvio.Commerce.PowerTools.Core.Pim;

namespace Truvio.Commerce.PowerTools.Tests;

/// <summary>Builders for PIM specs so each test states only what it cares about.</summary>
internal static class PimTestData
{
    public static ProductQuality Product(
        string productId = "PROD1",
        string number = "SKU-1",
        string name = "Bilge pump",
        int score = 100,
        string worstRule = "",
        IReadOnlyList<string>? missing = null,
        string languageId = "LANG1",
        IReadOnlyDictionary<string, int>? perLanguage = null,
        IReadOnlyList<ProductQuality>? variants = null) =>
        new(productId, string.Empty, languageId, number, name, score, worstRule, missing ?? [])
        {
            ScorePerLanguage = perLanguage ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            Variants = variants ?? []
        };

    public static RuleUsage Rule(
        int id = 1,
        string name = "Web ready",
        IReadOnlyList<string>? fields = null,
        bool excludeVariants = false,
        IReadOnlyList<string>? usages = null) =>
        new(id, name, fields ?? ["ShortDescription"], excludeVariants, usages ?? ["Shop 'Northwind'"]);

    public static VariantGap Gap(
        string productId = "PROD1",
        ulong potential = 12,
        int existing = 8,
        IReadOnlyList<string>? examples = null) =>
        new(productId, "SKU-1", "Bilge pump", potential, existing, examples ?? []);

    public static DuplicateAsset Duplicate(
        string productId = "PROD1",
        string path = "/Files/Images/pump.jpg",
        int count = 2) =>
        new(productId, "SKU-1", "Bilge pump", path, count);

    public static BrokenImage Broken(
        string productId = "PROD1",
        string path = "/Files/Images/missing.jpg") =>
        new(productId, "SKU-1", "Bilge pump", path);

    public static CategoryUsage Category(
        string id = "CAT1",
        string name = "Pumps",
        int groupCount = 1) =>
        new(id, name, groupCount);

    public static WorkflowUsage Workflow(
        string name = "Enrichment",
        bool byGroups = true,
        bool byProducts = false) =>
        new(name, byGroups, byProducts);

    /// <summary>A snapshot carrying only what the test names.</summary>
    public static PimSnapshot Snapshot(
        IReadOnlyList<ProductQuality>? products = null,
        IReadOnlyList<RuleUsage>? rules = null,
        IReadOnlyList<VariantGap>? gaps = null,
        IReadOnlyList<DuplicateAsset>? duplicates = null,
        IReadOnlyList<BrokenImage>? broken = null,
        IReadOnlyList<CategoryUsage>? categories = null,
        IReadOnlyList<string>? languages = null,
        int totalProductCount = 0,
        PimScope? scope = null) =>
        new(
            products ?? [],
            rules ?? [],
            gaps ?? [],
            duplicates ?? [],
            broken ?? [],
            categories ?? [],
            scope ?? PimScope.Default,
            totalProductCount)
        {
            Languages = languages ?? []
        };
}

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
