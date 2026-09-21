namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

/// <summary>An order-line (product) discount, reduced to the conditions DW checks before applying it to a product price.</summary>
public sealed record DiscountSpec
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool Active { get; init; }
    public int Priority { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public string ShopId { get; init; } = string.Empty;
    public string LanguageId { get; init; } = string.Empty;
    public bool AnonymousUsers { get; init; }
    public int? UserId { get; init; }
    public int? UserGroupId { get; init; }
    public string UserCustomerNumber { get; init; } = string.Empty;
    /// <summary>Free-text description of the discount's product scope ("all products", "2 products, 1 group", ...).</summary>
    public string ProductScope { get; init; } = string.Empty;
    /// <summary>True when the discount needs an order to be evaluated (order total, order field, product quantity &gt; 1, voucher).</summary>
    public bool NeedsOrder { get; init; }
    public string NeedsOrderReason { get; init; } = string.Empty;
    public string TypeDescription { get; init; } = string.Empty;
    public bool StopFurtherProcessing { get; init; }
    public bool OnlyApplyToNonDiscountedItems { get; init; }
}
