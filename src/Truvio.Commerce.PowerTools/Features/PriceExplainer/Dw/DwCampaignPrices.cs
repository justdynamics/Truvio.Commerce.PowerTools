using Dynamicweb.Ecommerce.Orders.Discounts;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Ecommerce.Products;
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Dw;

internal static class DwCampaignPrices
{
    public static DwPriceCalculation Calculate(PriceContext context, Product product, PriceInfo found,
        IReadOnlyList<DiscountInfo> infos, string selection, bool knownLegacyHost)
    {
        double? percentage = knownLegacyHost ? 0 : null;
        var allow = true;
#if DW_HAS_PRICE_DISCOUNT_PERCENTAGE
        allow = found.AllowOrderLineDiscounts;
        percentage = allow ? found.DiscountPercentage : 0;
#endif
        var amounts = new Dictionary<int, IPriceInfo>();
        var special = new HashSet<int>();
        if (allow)
        {
            foreach (var info in infos)
            {
                var d = info.Discount;
                if (!string.IsNullOrEmpty(d.ExtenderType) || (d.DiscountType != DiscountTypes.Amount && d.DiscountType != DiscountTypes.Percentage))
                {
                    amounts[d.ID] = info.GetPrice(context, product.DefaultUnitId, 0);
                    special.Add(d.ID);
                }
                else if (d.DiscountType != DiscountTypes.Percentage)
                    amounts[d.ID] = info.GetPrice(context, product.DefaultUnitId, 0);
                else
                    amounts[d.ID] = CalculateStandard(d, found, context);
            }
        }
        return Combine(found, percentage, amounts, selection, special);
    }

    internal static DwPriceCalculation Combine(PriceInfo found, double? percentage,
        IReadOnlyDictionary<int, IPriceInfo> amounts, string selection, IReadOnlySet<int>? special = null)
    {
        var selected = CampaignSelection.Select(amounts.ToDictionary(p => p.Key, p => p.Value.Price), selection);
        var applied = amounts.Where(p => selected.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value);
        var campaigns = new PriceInfo(found.Currency);
        foreach (var amount in applied.Values) campaigns = campaigns.Add(amount);
        var discount = found.Multiply(percentage is > 0 ? percentage.Value / 100 : 0);
        return new(found, percentage, discount, campaigns, found.Substract(discount).Substract(campaigns), applied, special ?? new HashSet<int>());
    }

    // DiscountProvider is internal even though CalculateDiscountPrice is public. Mirror its
    // standard percentage path using public PriceInfo operations, without reflection.
    private static PriceInfo CalculateStandard(Discount discount, PriceInfo price, PriceContext context)
    {
        var round = Dynamicweb.Configuration.SystemConfiguration.Instance
            .GetBoolean("/Globalsettings/Ecom/Price/RoundDiscountPerQuantity");
        var result = Percentage(price, discount.Percentage,
            round ? price.Currency.GetCultureInfo()?.NumberFormat.CurrencyDecimalDigits ?? 2 : null);
        var limit = discount.MaximumLimits?.FirstOrDefault(l => l.CurrencyCode == price.Currency.Code);
        if (limit is not null && result.PriceWithVAT > limit.Amount)
        {
            var amount = limit.Amount;
            if (!PriceHelper.PricesInDatabaseIncludeVat) amount *= 100 / (100 + result.VATPercent);
            var capped = PriceCalculated.Create(price.Currency, context.Country, new PriceRaw(amount, price.Currency));
            capped.Calculate();
            return capped;
        }
        return result;
    }

    internal static PriceInfo Percentage(PriceInfo price, double percentage, int? decimals) =>
        decimals is int places ? price.Multiply(percentage / 100, places) : price.Multiply(percentage / 100);
}
