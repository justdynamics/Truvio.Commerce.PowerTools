using Truvio.Commerce.PowerTools.Core.Diagnostics;

namespace Truvio.Commerce.PowerTools.Core.Pim.Rules;

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

/// <summary>
/// PIM-W8: the product resolves to an image path with no file behind it. Distinct from "no
/// image": DW happily stores a path to a file nobody uploaded, and the storefront then renders
/// a broken image rather than the no-picture fallback.
/// </summary>
public sealed class BrokenImageRule : IPimRule
{
    public const string Id = "PIM-W8";

    public IEnumerable<Finding> Evaluate(PimSnapshot snapshot)
    {
        foreach (var broken in snapshot.BrokenImages
                     .OrderBy(b => b.Number, StringComparer.OrdinalIgnoreCase))
        {
            yield return new Finding(
                Id,
                FindingSeverity.Warning,
                PimEntities.Product,
                broken.ProductId,
                Format.Product(broken.Number, broken.Name, broken.ProductId),
                "Image path has no file behind it",
                $"The product resolves to '{broken.Path}', which does not exist — the storefront renders a broken image instead of the no-picture fallback.",
                Subject: broken.Path);
        }
    }
}
