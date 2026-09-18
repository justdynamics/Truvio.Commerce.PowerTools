namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

public sealed class DiscountExplanation
{
    public required DiscountVerdict Verdict { get; init; }

    /// <summary>True when DW's DiscountInfoCollection actually applied the discount to this product.</summary>
    public bool AppliedByDw { get; init; }

    /// <summary>Discount amount DW computed (formatted), when applied.</summary>
    public string Amount { get; init; } = string.Empty;
}
