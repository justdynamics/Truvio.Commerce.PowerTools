using Truvio.Commerce.PowerTools.Features.PimQuality.Core;

namespace Truvio.Commerce.PowerTools.Tests.Features.PimQuality;

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
