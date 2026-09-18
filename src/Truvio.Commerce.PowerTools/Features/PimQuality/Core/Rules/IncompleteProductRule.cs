using System.Globalization;
using Truvio.Commerce.PowerTools.Features.PimQuality.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Core.Rules;

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
