using Dynamicweb.CoreUI.Data;
using Dynamicweb.Ecommerce;
using Dynamicweb.Ecommerce.Products;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Settings.Dw;
using Truvio.Commerce.PowerTools.Core.Settings;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// Product picker: DW's backend product search (number, name, id) in the default language,
/// variants included so a variant-specific price can be explained. AccountKey is carried
/// through to the explanation.
/// </summary>
public sealed class ProductPickQuery : DataQueryListBase<ProductPickModel, ProductPickModel, DataListViewModel<ProductPickModel>>
{
    private const int DefaultFetchCap = 200;

    public string AccountKey { get; set; } = string.Empty;

    protected override IEnumerable<ProductPickModel>? GetListItems()
    {
        var fetchCap = PowerToolsSettings.Positive(DwPowerToolsSettings.Current.ProductPickCap, DefaultFetchCap);
        var languageId = Services.Languages.GetDefaultLanguageId();
        var defaultCurrency = Services.Currencies.GetDefaultCurrency();

        var result = Services.Products.GetProductsBySearch(new ProductSearchFilter
        {
            SearchValue = Search ?? string.Empty,
            LanguageIds = [languageId],
            PageNumber = 1,
            PageSize = fetchCap,
            IncludeOrphanedProducts = true,
            VariantFilter = ProductSearchFilter.VariantStateFilter.All
        });

        var items = new List<ProductPickModel>();
        foreach (var product in result.Products)
        {
            items.Add(new ProductPickModel
            {
                ProductId = product.Id,
                VariantId = product.VariantId ?? string.Empty,
                LanguageId = product.LanguageId,
                IsActive = product.Active,
                Number = product.Number,
                Name = product.Name,
                Variant = string.IsNullOrEmpty(product.VariantId)
                    ? string.Empty
                    : $"{Services.Variants.GetVariantName(product.VariantId, languageId)} ({product.VariantId})",
                Active = product.Active ? "Yes" : "No",
                DefaultPrice = Services.Currencies.Format(defaultCurrency, product.DefaultPrice)
            });
        }

        if (result.TotalCount > fetchCap)
        {
            items.Add(new ProductPickModel
            {
                ProductId = string.Empty,
                Number = "...",
                Name = $"{result.TotalCount - fetchCap} more products not shown - use the search to narrow the list"
            });
        }

        return items;
    }

    protected override IEnumerable<ProductPickModel> MapModels(IEnumerable<ProductPickModel> items) => items;

    protected override DataListViewModel<ProductPickModel> MakeListModel() => new();
}
