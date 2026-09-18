namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

/// <summary>The selection DW evaluates price rows against (PriceContext + PriceProductSelection).</summary>
public sealed record PriceLookupContext
{
    public int? UserId { get; init; }
    public string? UserCustomerNumber { get; init; }
    public IReadOnlySet<int> UserGroupIds { get; init; } = new HashSet<int>();
    public IReadOnlySet<string> UserGroupCustomerNumbers { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public string CurrencyCode { get; init; } = string.Empty;
    public string? CountryCode { get; init; }
    public string? ShopId { get; init; }
    public string LanguageId { get; init; } = string.Empty;
    public string VariantId { get; init; } = string.Empty;
    public string VirtualVariantId { get; init; } = string.Empty;
    public string UnitId { get; init; } = string.Empty;
    public long StockLocationId { get; init; }
    public double Quantity { get; init; } = 1;
    public double QuantityAllVariants { get; init; }
    public DateTime Time { get; init; } = DateTime.Now;
    /// <summary>VAT percent used to normalise rows stored with VAT when the DB stores prices without VAT.</summary>
    public double VatPercent { get; init; }
    public bool PricesInDatabaseIncludeVat { get; init; }
}
