namespace Truvio.Commerce.PowerTools.Features.PimQuality.Core;

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
