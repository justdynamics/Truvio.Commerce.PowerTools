using System.Globalization;
using Truvio.Commerce.PowerTools.Features.PimQuality.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Core.Rules;

/// <summary>
/// PIM-W2: the field missing on the most products. The differentiator of the whole section —
/// a score tells you the catalog is bad, this tells you which single field to fill first.
/// </summary>
public sealed class CommonFieldGapRule(int percentThreshold) : IPimRule
{
    public const string Id = "PIM-W2";

    /// <summary>Share of scanned products (percent) missing one field before it is reported.</summary>
    public int PercentThreshold { get; } = percentThreshold;

    public IEnumerable<Finding> Evaluate(PimSnapshot snapshot)
    {
        if (snapshot.Products.Count == 0)
            yield break;

        var scanned = snapshot.Products.Count;

        foreach (var gap in Rank(snapshot))
        {
            var percent = gap.Count * 100d / scanned;
            if (percent < PercentThreshold)
                continue;

            yield return new Finding(
                Id,
                FindingSeverity.Info,
                PimEntities.ProductField,
                gap.Field,
                gap.Field,
                $"Missing on {gap.Count} of {scanned} scanned products ({percent:0.#}%)",
                $"Filling '{gap.Field}' is the single change that lifts the most products in this scope." +
                (snapshot.IsTruncated
                    ? $" Measured over the {scanned} products scanned, not the whole catalog of {snapshot.TotalProductCount}."
                    : string.Empty),
                Subject: gap.Field);
        }
    }

    /// <summary>Every missing field with its product count, most common first.</summary>
    public static IReadOnlyList<(string Field, int Count)> Rank(PimSnapshot snapshot) =>
        snapshot.Products
            .SelectMany(p => p.MissingFields.Distinct(StringComparer.OrdinalIgnoreCase))
            .GroupBy(f => f, StringComparer.OrdinalIgnoreCase)
            .Select(g => (Field: g.Key, Count: g.Count()))
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.Field, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
