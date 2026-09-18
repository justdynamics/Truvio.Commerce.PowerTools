using System.Globalization;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Core.Rules;

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
