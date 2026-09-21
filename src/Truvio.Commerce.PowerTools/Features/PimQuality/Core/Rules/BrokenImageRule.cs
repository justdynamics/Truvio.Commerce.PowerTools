using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Core.Rules;

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
