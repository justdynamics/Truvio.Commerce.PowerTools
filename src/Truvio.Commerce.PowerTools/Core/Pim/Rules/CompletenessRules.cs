using System.Globalization;
using Truvio.Commerce.PowerTools.Core.Diagnostics;

namespace Truvio.Commerce.PowerTools.Core.Pim.Rules;

/// <summary>
/// PIM-W1: a product scoring below the threshold. One finding per product, worst first — this
/// is the list the catalog owner works down.
/// </summary>
public sealed class IncompleteProductRule(int threshold) : IPimRule
{
    public const string Id = "PIM-W1";

    /// <summary>Below this completeness score a product is reported.</summary>
    public int Threshold { get; } = threshold;

    public IEnumerable<Finding> Evaluate(PimSnapshot snapshot)
    {
        foreach (var product in snapshot.Products
                     .Where(p => p.Score < Threshold)
                     .OrderBy(p => p.Score)
                     .ThenBy(p => p.Number, StringComparer.OrdinalIgnoreCase))
        {
            var missing = product.MissingFields.Count == 0
                ? "DW reports no specific missing field — check which completion rules apply to this product"
                : $"Missing: {Format.List(product.MissingFields, 5)}";

            yield return new Finding(
                Id,
                product.Score == 0 ? FindingSeverity.Critical : FindingSeverity.Warning,
                PimEntities.Product,
                product.ProductId,
                Format.Product(product),
                $"Completeness {product.Score}% is below the {Threshold}% threshold",
                missing);
        }
    }
}

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

/// <summary>
/// PIM-W3: a language layer materially behind the default one. Reported per language, not per
/// product — "Danish is 40 points behind" is the actionable sentence; 400 product rows are not.
/// </summary>
public sealed class LanguageGapRule(int pointsThreshold) : IPimRule
{
    public const string Id = "PIM-W3";

    /// <summary>Score points a language may trail the default by before it is reported.</summary>
    public int PointsThreshold { get; } = pointsThreshold;

    public IEnumerable<Finding> Evaluate(PimSnapshot snapshot)
    {
        var defaultLanguage = snapshot.Languages.FirstOrDefault();
        if (string.IsNullOrEmpty(defaultLanguage) || snapshot.Products.Count == 0)
            yield break;

        foreach (var language in snapshot.Languages.Skip(1))
        {
            var pairs = snapshot.Products
                .Where(p => p.ScorePerLanguage.ContainsKey(defaultLanguage) && p.ScorePerLanguage.ContainsKey(language))
                .ToList();

            if (pairs.Count == 0)
                continue;

            var baseline = pairs.Average(p => p.ScorePerLanguage[defaultLanguage]);
            var actual = pairs.Average(p => p.ScorePerLanguage[language]);
            var delta = baseline - actual;
            if (delta < PointsThreshold)
                continue;

            var worst = pairs
                .OrderBy(p => p.ScorePerLanguage[language] - p.ScorePerLanguage[defaultLanguage])
                .Take(3)
                .Select(Format.Product)
                .ToList();

            yield return new Finding(
                Id,
                FindingSeverity.Warning,
                PimEntities.Language,
                language,
                language,
                $"Language layer averages {actual:0}% against {baseline:0}% in {defaultLanguage} ({delta:0} points behind)",
                $"Measured over {pairs.Count} product(s). Furthest behind: {string.Join("; ", worst)}.",
                Subject: language);
        }
    }
}

/// <summary>Shared formatting so every PIM finding names things the same way.</summary>
internal static class Format
{
    /// <summary>"FTT-SNK-03 - Beef Jerky", falling back to whatever identity exists.</summary>
    public static string Product(ProductQuality product) =>
        (product.Number, product.Name) switch
        {
            ("", "") => product.ProductId,
            ("", var name) => name,
            (var number, "") => number,
            var (number, name) => $"{number} - {name}"
        };

    public static string Product(string number, string name, string productId) =>
        (number, name) switch
        {
            ("", "") => productId,
            ("", _) => name,
            (_, "") => number,
            _ => $"{number} - {name}"
        };

    /// <summary>"a, b, c +4 more" — long lists are truncated, never dumped.</summary>
    public static string List(IReadOnlyList<string> values, int max)
    {
        if (values.Count == 0)
            return "none";

        var shown = string.Join(", ", values.Take(max));
        var rest = values.Count - Math.Min(max, values.Count);
        return rest > 0 ? $"{shown} +{rest} more" : shown;
    }

    public static string Number(ulong value) => value.ToString("N0", CultureInfo.InvariantCulture);
}
