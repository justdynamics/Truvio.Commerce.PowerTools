using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Core.Rules;

/// <summary>
/// PIM-W7: a product category no group uses. Categories are the field containers of the PIM
/// model, so an unused one usually means a modelling change was abandoned half-way — the
/// fields still exist and still show up in field pickers.
/// </summary>
public sealed class UnusedCategoryRule : IPimRule
{
    public const string Id = "PIM-W7";

    public IEnumerable<Finding> Evaluate(PimSnapshot snapshot)
    {
        foreach (var category in snapshot.Categories
                     .Where(c => c.IsUnused)
                     .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
        {
            yield return new Finding(
                Id,
                FindingSeverity.Info,
                PimEntities.Category,
                category.CategoryId,
                string.IsNullOrEmpty(category.Name) ? category.CategoryId : category.Name,
                "Category is used by no product group",
                "No group references this category, so its fields reach no product — but they still appear in field pickers and completion rules.");
        }
    }
}
