namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

public static class CampaignSelection
{
    /// <summary>Amounts are positive savings. Ties keep DW's first enumerated discount.</summary>
    public static IReadOnlySet<int> Select(IReadOnlyDictionary<int, double> amounts, string behavior)
    {
        if (amounts.Count == 0) return new HashSet<int>();
        return behavior.ToLowerInvariant() switch
        {
            "high" => new HashSet<int> { amounts.MaxBy(p => p.Value).Key },
            "low" => new HashSet<int> { amounts.MinBy(p => p.Value).Key },
            _ => amounts.Keys.ToHashSet()
        };
    }
}
