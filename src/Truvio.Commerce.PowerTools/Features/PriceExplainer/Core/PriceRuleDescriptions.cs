namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

internal static class PriceRuleDescriptions
{
    public static string Describe(PriceMatrixVerdict matrix)
    {
        var rule = matrix.Rule switch
        {
            PriceSelectionRule.LowestAmount => "Lowest matching price",
            PriceSelectionRule.LowestNet => "Lowest net price after found discount",
            PriceSelectionRule.PriorityThenLowestNet => "Highest priority, then lowest net price",
            PriceSelectionRule.GroupsPriorityThenLowestNet => "Highest priority, then lowest net price (including price and discount groups)",
            _ => "Unverified price selection"
        };
        var sources = matrix.Rows.Where(v => v.Matches && v.Row.DiscountPercentage == matrix.FallbackDiscountPercentage
            && (matrix.Rule == PriceSelectionRule.GroupsPriorityThenLowestNet || v.ComparableAmount == 0)).Select(v => v.Row.Id).ToList();
        var fallback = sources.Count == 0 ? "no fallback row"
            : $"fallback {matrix.FallbackDiscountPercentage:0.##}% from matching rows {string.Join(", ", sources.Take(5))}{(sources.Count > 5 ? " (more sources omitted)" : "")}";
        return $"{rule}; {fallback}. " + (matrix.Winner is { } winning
            ? $"Row {winning.Row.Id}: own {winning.Row.DiscountPercentage:0.##}%, effective {winning.EffectiveDiscountPercentage:0.##}%."
            : "Product default price fallback.");
    }
}
