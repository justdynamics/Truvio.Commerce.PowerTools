namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

/// <summary>Verified DefaultPriceProvider algorithms; Unknown never names a winner.</summary>
public enum PriceSelectionRule
{
    Unknown,
    LowestAmount,
    LowestNet,
    PriorityThenLowestNet,
    GroupsPriorityThenLowestNet
}
