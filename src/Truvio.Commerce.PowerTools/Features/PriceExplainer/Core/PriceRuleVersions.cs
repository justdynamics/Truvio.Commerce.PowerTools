namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

public static class PriceRuleVersions
{
    // Deliberately exact: uninspected releases must not inherit a guessed boundary.
    public static PriceSelectionRule Resolve(Version version, bool discountApi, bool groupApi)
    {
        var release = $"{version.Major}.{version.Minor}.{version.Build}";
        return release switch
        {
            "10.8.4" => PriceSelectionRule.LowestAmount,
            "10.17.5" when discountApi => PriceSelectionRule.LowestNet,
            "10.25.5" or "10.28.1" or "10.28.17" or "10.29.8" when discountApi => PriceSelectionRule.PriorityThenLowestNet,
            "10.30.6" or "10.30.8" when discountApi && groupApi => PriceSelectionRule.GroupsPriorityThenLowestNet,
            _ => PriceSelectionRule.Unknown
        };
    }
}
