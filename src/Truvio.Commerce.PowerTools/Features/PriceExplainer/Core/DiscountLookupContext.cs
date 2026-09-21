namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

public sealed record DiscountLookupContext
{
    public int? UserId { get; init; }
    public string? UserCustomerNumber { get; init; }
    public IReadOnlySet<int> UserGroupIds { get; init; } = new HashSet<int>();
    public string CurrencyCode { get; init; } = string.Empty;
    public string? CountryCode { get; init; }
    public string? ShopId { get; init; }
    public string LanguageId { get; init; } = string.Empty;
    public DateTime Time { get; init; } = DateTime.Now;
}
