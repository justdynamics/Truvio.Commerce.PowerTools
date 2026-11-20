namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

/// <summary>
/// One row of the price matrix (EcomPrices), reduced to the fields DW's price filters read.
/// Empty string / 0 / null means "not restricted" for every dimension.
/// </summary>
public sealed record PriceRowSpec
{
    public string ProductId { get; init; } = string.Empty;
    public int Priority { get; init; }
    public double DiscountPercentage { get; init; }
    public bool AllowOrderDiscounts { get; init; } = true;
    public bool AllowOrderLineDiscounts { get; init; } = true;
    public string CustomerPriceGroupId { get; init; } = string.Empty;
    public string CustomerDiscountGroupId { get; init; } = string.Empty;
    public string ProductDiscountGroupId { get; init; } = string.Empty;
    public bool HasValidConfiguration { get; init; } = true;
    public string TargetKind => !string.IsNullOrEmpty(UserId) || !string.IsNullOrEmpty(UserCustomerNumber) ? "This customer"
        : !string.IsNullOrEmpty(CustomerPriceGroupId) ? "Customer price group"
        : !string.IsNullOrEmpty(CustomerDiscountGroupId) ? "Customer discount group" + (!string.IsNullOrEmpty(ProductDiscountGroupId) ? " x product discount group" : "")
        : !string.IsNullOrEmpty(ProductDiscountGroupId) ? "Product discount group"
        : !string.IsNullOrEmpty(UserGroupId) || !string.IsNullOrEmpty(CustomerGroupId) ? "User group" : "List price";
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
