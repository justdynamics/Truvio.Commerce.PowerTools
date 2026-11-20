#if !DW_HAS_PRICE_GROUPS
using Dynamicweb.Ecommerce;
#else
using Dynamicweb.Ecommerce.Prices;
#endif
using Dynamicweb.Ecommerce.Products;
#if DW_HAS_PRICE_GROUPS
using Dynamicweb.Data;
#endif
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Dw;

internal static class DwPriceRows
{
    public static List<PriceRowSpec> Read(Product product)
    {
#if DW_HAS_PRICE_GROUPS
        // The public product lookup excludes discount-group rows. Read only these two targets.
        var prices = new List<Price>();
        var sql = CommandBuilder.Create("SELECT * FROM EcomPrices WHERE PriceProductId = {0} OR ({1} <> '' AND PriceProductDiscountGroupId = {1})", product.Id, product.DiscountGroupId ?? string.Empty);
        using var dataRecord = Database.CreateDataReader(sql);
        while (dataRecord.Read())
            prices.Add(new Price
            {
                Id = Dynamicweb.Core.Converter.ToString(dataRecord["PriceId"]),
                ProductId = Dynamicweb.Core.Converter.ToString(dataRecord["PriceProductId"]),
                VariantId = Dynamicweb.Core.Converter.ToString(dataRecord["PriceProductVariantID"]),
                LanguageId = Dynamicweb.Core.Converter.ToString(dataRecord["PriceProductLanguageID"]),
                UnitId = Dynamicweb.Core.Converter.ToString(dataRecord["PriceUnitID"]),
                CurrencyCode = Dynamicweb.Core.Converter.ToString(dataRecord["PriceCurrency"]),
                Quantity = Dynamicweb.Core.Converter.ToDouble(dataRecord["PriceQuantity"]),
                Amount = Dynamicweb.Core.Converter.ToDouble(dataRecord["PriceAmount"]),
                IsWithVat = Dynamicweb.Core.Converter.ToBoolean(dataRecord["PriceIsWithVat"]),
                CustomerGroupId = Dynamicweb.Core.Converter.ToString(dataRecord["PriceCustomerGroupID"]),
                Priority = Dynamicweb.Core.Converter.ToInt32(dataRecord["PricePriority"]),
                CountryCode = Dynamicweb.Core.Converter.ToString(dataRecord["PriceCountry"]).TrimEnd(),
                ShopId = Dynamicweb.Core.Converter.ToString(dataRecord["PriceShopID"]),
                ValidFrom = Dynamicweb.Core.Converter.ToNullableDateTime(dataRecord["PriceValidFrom"], (DateTime?)null),
                ValidTo = Dynamicweb.Core.Converter.ToNullableDateTime(dataRecord["PriceValidTo"], (DateTime?)null),
                UserId = Dynamicweb.Core.Converter.ToString(dataRecord["PriceUserID"]),
                UserGroupId = Dynamicweb.Core.Converter.ToString(dataRecord["PriceUserGroupID"]),
                UserCustomerNumber = Dynamicweb.Core.Converter.ToString(dataRecord["PriceUserCustomerNumber"]),
                IsInformative = Dynamicweb.Core.Converter.ToBoolean(dataRecord["PriceIsInformative"]),
                StockLocationId = Dynamicweb.Core.Converter.ToInt64(dataRecord["PriceStockLocationID"]),
                ExternalId = Dynamicweb.Core.Converter.ToString(dataRecord["PriceExternalId"]),
                DiscountPercentage = Dynamicweb.Core.Converter.ToDouble(dataRecord["PriceDiscountPercentage"]),
                AllowOrderLineDiscounts = Dynamicweb.Core.Converter.ToBoolean(dataRecord["PriceAllowOrderLineDiscounts"]),
                AllowOrderDiscounts = Dynamicweb.Core.Converter.ToBoolean(dataRecord["PriceAllowOrderDiscounts"]),
                CustomerDiscountGroupId = Dynamicweb.Core.Converter.ToNullableString(dataRecord["PriceCustomerDiscountGroupId"]),
                CustomerPriceGroupId = Dynamicweb.Core.Converter.ToNullableString(dataRecord["PriceCustomerPriceGroupId"]),
                ProductDiscountGroupId = Dynamicweb.Core.Converter.ToNullableString(dataRecord["PriceProductDiscountGroupId"])
            });
#else
        var prices = Services.Prices.GetByProductId(product.Id);
#endif
        return prices.Select(p => new PriceRowSpec
            {
                Id = p.Id,
                ProductId = p.ProductId ?? string.Empty,
                Priority = p.Priority,
#if DW_HAS_PRICE_DISCOUNT_PERCENTAGE
                DiscountPercentage = p.DiscountPercentage,
                AllowOrderDiscounts = p.AllowOrderDiscounts,
                AllowOrderLineDiscounts = p.AllowOrderLineDiscounts,
#endif
#if DW_HAS_PRICE_GROUPS
                CustomerPriceGroupId = p.CustomerPriceGroupId ?? string.Empty,
                CustomerDiscountGroupId = p.CustomerDiscountGroupId ?? string.Empty,
                ProductDiscountGroupId = p.ProductDiscountGroupId ?? string.Empty,
                HasValidConfiguration = p.HasValidConfiguration,
#endif
                VariantId = p.VariantId ?? string.Empty,
                LanguageId = p.LanguageId ?? string.Empty,
                UnitId = p.UnitId ?? string.Empty,
                CurrencyCode = p.CurrencyCode ?? string.Empty,
                CountryCode = p.CountryCode ?? string.Empty,
                ShopId = p.ShopId ?? string.Empty,
                Quantity = p.Quantity,
                Amount = p.Amount,
                IsWithVat = p.IsWithVat,
                IsInformative = p.IsInformative,
                StockLocationId = p.StockLocationId,
                ValidFrom = p.ValidFrom,
                ValidTo = p.ValidTo,
                UserId = p.UserId ?? string.Empty,
                UserCustomerNumber = p.UserCustomerNumber ?? string.Empty,
                UserGroupId = p.UserGroupId ?? string.Empty,
                CustomerGroupId = p.CustomerGroupId ?? string.Empty
            })
            .ToList();

    }
}
