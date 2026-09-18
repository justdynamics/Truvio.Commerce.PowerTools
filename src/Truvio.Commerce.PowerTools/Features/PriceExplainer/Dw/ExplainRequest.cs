namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Dw;

/// <summary>The inputs of one explanation: who, what, and under which commercial context.</summary>
public sealed record ExplainRequest
{
    /// <summary>null = anonymous visitor.</summary>
    public int? UserId { get; init; }
    public string ProductId { get; init; } = string.Empty;
    public string VariantId { get; init; } = string.Empty;
    public string LanguageId { get; init; } = string.Empty;
    public string CurrencyCode { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public string ShopId { get; init; } = string.Empty;
    public double Quantity { get; init; } = 1;
    public DateTime? Time { get; init; }
}
