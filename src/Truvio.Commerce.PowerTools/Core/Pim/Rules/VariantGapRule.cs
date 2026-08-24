using Truvio.Commerce.PowerTools.Core.Diagnostics;

namespace Truvio.Commerce.PowerTools.Core.Pim.Rules;

/// <summary>
/// PIM-W4: a product whose variant groups allow combinations the catalog does not have.
/// <para>
/// The count comes from DW (<c>VariantService.PotentialVariantCount</c>) and is deliberately
/// never derived by enumerating: three groups of ten options are a thousand combinations, and
/// a real catalog has products where that number runs into the millions. A gap that large is
/// almost always intentional (nobody stocks every combination), so the finding stays a
/// Warning and says the number rather than pretending every combination is missing data.
/// </para>
/// </summary>
public sealed class VariantGapRule : IPimRule
{
    public const string Id = "PIM-W4";

    /// <summary>
    /// Above this many potential combinations the gap is reported as a number only — the
    /// source never enumerates examples, and a "fill these in" message would be nonsense.
    /// </summary>
    public const ulong LargeCombinationCount = 1000;

    public IEnumerable<Finding> Evaluate(PimSnapshot snapshot)
    {
        foreach (var gap in snapshot.VariantGaps
                     .Where(g => g.HasGap)
                     .OrderByDescending(g => g.MissingCount)
                     .ThenBy(g => g.Number, StringComparer.OrdinalIgnoreCase))
        {
            var detail = gap.MissingExamples.Count > 0
                ? $"Missing, for example: {Format.List(gap.MissingExamples, 5)}."
                : gap.PotentialCount > LargeCombinationCount
                    ? "Too many potential combinations to list — a gap this size is usually deliberate; confirm the variant groups are the intended ones."
                    : "DW reports no combination ids for the missing entries.";

            yield return new Finding(
                Id,
                FindingSeverity.Warning,
                PimEntities.Product,
                gap.ProductId,
                Format.Product(gap.Number, gap.Name, gap.ProductId),
                $"{Format.Number(gap.MissingCount)} of {Format.Number(gap.PotentialCount)} variant combinations do not exist",
                $"{gap.ExistingCount} combination(s) exist. {detail}");
        }
    }
}
