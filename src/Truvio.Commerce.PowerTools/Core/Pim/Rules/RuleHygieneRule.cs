using Truvio.Commerce.PowerTools.Core.Diagnostics;

namespace Truvio.Commerce.PowerTools.Core.Pim.Rules;

/// <summary>
/// PIM-W6: a completion rule assigned to no shop, group or query. It scores nothing, so it is
/// invisible — an editor maintaining the rule believes it governs the catalog while it governs
/// nothing. The most common cause is a group or shop that was deleted after the assignment.
/// </summary>
public sealed class DeadCompletionRuleRule : IPimRule
{
    public const string Id = "PIM-W6";

    public IEnumerable<Finding> Evaluate(PimSnapshot snapshot)
    {
        foreach (var rule in snapshot.Rules
                     .Where(r => r.IsDead)
                     .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
        {
            var fields = rule.FieldSystemNames.Count == 0
                ? "It names no fields either."
                : $"It requires {Format.List(rule.FieldSystemNames, 5)}.";

            yield return new Finding(
                Id,
                FindingSeverity.Warning,
                PimEntities.CompletionRule,
                rule.RuleId.ToString(),
                rule.Name,
                "Completion rule is assigned to no shop, group or query",
                $"The rule scores nothing at all — no product is measured against it. {fields} " +
                "Assign it under Settings > Commerce, or delete it.");
        }
    }
}

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
