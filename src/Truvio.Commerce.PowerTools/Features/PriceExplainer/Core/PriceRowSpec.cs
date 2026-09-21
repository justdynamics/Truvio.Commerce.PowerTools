namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

/// <summary>
/// One row of the price matrix (EcomPrices), reduced to the fields DW's price filters read.
/// Empty string / 0 / null means "not restricted" for every dimension.
/// </summary>
public sealed record PriceRowSpec
{
    public string Id { get; init; } = string.Empty;
    public string VariantId { get; init; } = string.Empty;
    public string LanguageId { get; init; } = string.Empty;
    public string UnitId { get; init; } = string.Empty;
    public string CurrencyCode { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public string ShopId { get; init; } = string.Empty;
    public double Quantity { get; init; }
    public double Amount { get; init; }
    public bool IsWithVat { get; init; }
    public bool IsInformative { get; init; }
    public long StockLocationId { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string UserCustomerNumber { get; init; } = string.Empty;
    public string UserGroupId { get; init; } = string.Empty;
    /// <summary>Legacy customer-group column: matches a user group by its customer number.</summary>
    public string CustomerGroupId { get; init; } = string.Empty;
}
