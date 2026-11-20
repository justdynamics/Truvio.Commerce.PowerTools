namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

public sealed class DiscountExplanation
{
    public required DiscountVerdict Verdict { get; init; }

    /// <summary>True when a DW-valid discount survives allow flags and the configured selection behavior.</summary>
    public bool AppliedByDw { get; init; }

    /// <summary>Discount amount DW computed (formatted), when applied.</summary>
    public string Amount { get; init; } = string.Empty;
    public string CalculationNote { get; init; } = string.Empty;
}
