using Truvio.Commerce.PowerTools.Features.PimQuality.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Core.Rules;

/// <summary>
/// PIM-W5: the same asset attached to a product more than once. Harmless to the database and
/// invisible in the backend, but it duplicates the asset in every frontend gallery — which is
/// why it is Info, not Warning.
/// </summary>
public sealed class DuplicateAssetRule : IPimRule
{
    public const string Id = "PIM-W5";

    public IEnumerable<Finding> Evaluate(PimSnapshot snapshot)
    {
        foreach (var duplicate in snapshot.DuplicateAssets
                     .Where(d => d.Count > 1)
                     .OrderByDescending(d => d.Count)
                     .ThenBy(d => d.Number, StringComparer.OrdinalIgnoreCase))
        {
            yield return new Finding(
                Id,
                FindingSeverity.Info,
                PimEntities.Product,
                duplicate.ProductId,
                Format.Product(duplicate.Number, duplicate.Name, duplicate.ProductId),
                $"Asset attached {duplicate.Count} times",
                $"'{duplicate.Path}' appears {duplicate.Count} times on this product — every frontend gallery repeats it.",
                Subject: duplicate.Path);
        }
    }
}
