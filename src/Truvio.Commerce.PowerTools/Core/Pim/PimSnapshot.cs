using Truvio.Commerce.PowerTools.Core.Diagnostics;

namespace Truvio.Commerce.PowerTools.Core.Pim;

/// <summary>
/// One product family's completeness, as the rules see it. A family row carries the master
/// product's score; <see cref="Variants"/> holds the same shape per variant so the drill-down
/// screen can explain a family without a second scan.
/// <para>
/// Scores are DW's own (<c>CompletionRuleService</c>), never recomputed here — PowerTools
/// aggregates what DW computes, it does not invent a second definition of "complete".
/// </para>
/// </summary>
/// <param name="Score">0-100, DW's completeness value for this product in this language.</param>
/// <param name="MissingFields">
/// Field system names DW reports as empty AND in scope — a field excluded from the calculation
/// (inherited, out of scope for the rule) is never listed, or every product looks broken.
/// </param>
public sealed record ProductQuality(
    string ProductId,
    string VariantId,
    string LanguageId,
    string Number,
    string Name,
    int Score,
    string WorstRule,
    IReadOnlyList<string> MissingFields)
{
    /// <summary>Per-variant rows for the same family; empty on a product without variants.</summary>
    public IReadOnlyList<ProductQuality> Variants { get; init; } = [];

    /// <summary>Score in every language scanned, keyed by language id — drives PIM-W3.</summary>
    public IReadOnlyDictionary<string, int> ScorePerLanguage { get; init; } =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public bool IsVariant => !string.IsNullOrEmpty(VariantId);

    public string DisplayName => string.IsNullOrEmpty(Name) ? ProductId : Name;
}

/// <summary>A completion rule and everything that references it; no references = dead config.</summary>
/// <param name="Usages">Human-readable "Shop 'Northwind'" / "Group 'Engines'" strings.</param>
public sealed record RuleUsage(
    int RuleId,
    string Name,
    IReadOnlyList<string> FieldSystemNames,
    bool ExcludeVariants,
    IReadOnlyList<string> Usages)
{
    public bool IsDead => Usages.Count == 0;
}

/// <summary>
/// A product whose variant groups allow more combinations than actually exist.
/// <para>
/// <paramref name="PotentialCount"/> is DW's own count and can be astronomically large; when it
/// exceeds what is safe to enumerate, <paramref name="MissingExamples"/> stays empty and only
/// the number is reported.
/// </para>
/// </summary>
public sealed record VariantGap(
    string ProductId,
    string Number,
    string Name,
    ulong PotentialCount,
    int ExistingCount,
    IReadOnlyList<string> MissingExamples)
{
    /// <summary>Combinations the catalog does not have. Never negative — DW's potential count is a ceiling.</summary>
    public ulong MissingCount =>
        PotentialCount > (ulong)ExistingCount ? PotentialCount - (ulong)ExistingCount : 0;

    public bool HasGap => MissingCount > 0;
}

/// <summary>The same asset path attached to one product more than once.</summary>
public sealed record DuplicateAsset(
    string ProductId,
    string Number,
    string Name,
    string Path,
    int Count);

/// <summary>A product whose resolved image path has no file behind it.</summary>
public sealed record BrokenImage(
    string ProductId,
    string Number,
    string Name,
    string Path);

/// <summary>A product category and how many groups use it; zero = nothing references it.</summary>
public sealed record CategoryUsage(string CategoryId, string Name, int GroupCount)
{
    public bool IsUnused => GroupCount == 0;
}

/// <summary>A workflow referenced by groups or products — the governance screen's rows.</summary>
public sealed record WorkflowUsage(string Name, bool UsedByGroups, bool UsedByProducts)
{
    public bool IsReferenced => UsedByGroups || UsedByProducts;
}

/// <summary>
/// One consistent read of the catalog that every rule evaluates against. Taking a snapshot
/// first (instead of letting each rule query DW) keeps the rules pure and makes the counts on
/// the overview agree with the rows on the list screens — the same contract
/// <see cref="Operations.OperationsSnapshot"/> follows.
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

/// <summary>A rule over one <see cref="PimSnapshot"/>. Rule ids stay stable: PIM-W1..</summary>
public interface IPimRule
{
    IEnumerable<Finding> Evaluate(PimSnapshot snapshot);
}

/// <summary>Entity names the PIM findings are about — the <see cref="Finding.EntityName"/> values.</summary>
public static class PimEntities
{
    public const string Product = "Product";
    public const string ProductField = "ProductField";
    public const string CompletionRule = "CompletionRule";
    public const string Category = "ProductCategory";
    public const string Language = "EcomLanguage";
}
