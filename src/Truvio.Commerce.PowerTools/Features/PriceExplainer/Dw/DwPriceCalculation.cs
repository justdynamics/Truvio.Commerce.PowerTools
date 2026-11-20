using Dynamicweb.Ecommerce.Prices;

namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Dw;

internal sealed record DwPriceCalculation(double? Percentage, PriceInfo FoundDiscount,
    PriceInfo Campaigns, PriceInfo Final, IReadOnlyDictionary<int, IPriceInfo> Applied,
    IReadOnlySet<int> QuantityOneAmounts);
