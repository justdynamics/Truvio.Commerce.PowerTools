using Truvio.Commerce.PowerTools.Shared.Currency;

namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

/// <summary>Everything the Price Explainer screen renders for one account + product + context.</summary>
public sealed class ExplainReport
{
    public bool DiscountDetailsAvailable { get; init; }
    public string FoundDiscount { get; init; } = string.Empty;
    public string FoundDiscountReason { get; init; } = string.Empty;
    public string LineTotal { get; init; } = string.Empty;
    public string AllowFlags { get; init; } = string.Empty;
    public IReadOnlyList<PriceStep> Steps { get; init; } = [];
    public IReadOnlyDictionary<string, int> DiscountRejectedCounts { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, List<DiscountExplanation>> DiscountRejectedSamples { get; init; } = new Dictionary<string, List<DiscountExplanation>>();
    public int ApplicableDiscountCount { get; init; }
    public IReadOnlyList<(string Label, string Value)> Context { get; init; } = [];

    public VisibilityVerdict Visibility { get; init; } = new();

    public PriceMatrixVerdict PriceMatrix { get; init; } = new();

    /// <summary>The price DW itself computed (PriceManager.FindPrice at the selected quantity) before discounts, formatted.</summary>
    public string DwPriceBeforeDiscount { get; init; } = string.Empty;

    /// <summary>"Price matrix" / "Product default price" / custom provider name.</summary>
    public string DwPriceSource { get; init; } = string.Empty;

    public string ProductDefaultPrice { get; init; } = string.Empty;

    public IReadOnlyList<DiscountExplanation> Discounts { get; init; } = [];

    public string DiscountSelectionBehavior { get; init; } = string.Empty;

    public string DwDiscountTotal { get; init; } = string.Empty;

    public string DwFinalPrice { get; init; } = string.Empty;

    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>How the shown price was converted out of the default currency; null = no conversion.</summary>
    public ConversionExplanation? Conversion { get; init; }

    /// <summary>Quantity price breaks DW reports for the product in this context.</summary>
    public IReadOnlyList<(string Quantity, string Price)> QuantityPrices { get; init; } = [];
}
