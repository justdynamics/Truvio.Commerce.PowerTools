namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

internal static class PriceRuleDescriptions
{
    public static string Describe(PriceSelectionRule rule) => rule switch
    {
        PriceSelectionRule.LowestAmount => "Lowest matching price",
        PriceSelectionRule.LowestNet => "Lowest net price after found discount",
        PriceSelectionRule.PriorityThenLowestNet => "Highest priority, then lowest net price",
        PriceSelectionRule.GroupsPriorityThenLowestNet => "Highest priority, then lowest net price (including price and discount groups)",
        _ => "Unverified price selection"
    };
}
