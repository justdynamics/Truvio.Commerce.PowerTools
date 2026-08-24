using System.Collections.Concurrent;
using Truvio.Commerce.PowerTools.Core.Settings.Dw;

namespace Truvio.Commerce.PowerTools.Core.Commerce.Dw;

/// <summary>
/// The DW-facing half of "Preview in shop": resolves the product-detail page for a shop —
/// the settings mapping first (<see cref="PdpPreview.ResolvePageId"/>), then auto-detection:
/// a website bound to the shop (<c>Area.EcomShopId</c>) whose pages include a Swift
/// product-details page. Auto-detection is cached per shop for the process lifetime; a
/// changed page structure needs a restart or an explicit settings mapping (which always
/// wins and is read live). Null everywhere something is missing — the button simply
/// does not render.
/// </summary>
public static class DwPdpLocator
{
    /// <summary>Item types that mark a page as a product-detail page, in preference order.</summary>
    private static readonly string[] PdpItemTypes = ["Swift-v2_ProductDetails", "Swift_ProductDetailPage"];

    private static readonly ConcurrentDictionary<string, int?> DetectedPages = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The preview URL for a product in a shop's storefront, or null when unresolvable.</summary>
    public static string? UrlFor(string? shopId, string? productId, string? variantId = null)
    {
        if (string.IsNullOrEmpty(productId))
            return null;

        try
        {
            var configured = PdpPreview.ResolvePageId(DwPowerToolsSettings.Current.PreviewPages, shopId);
            var pageId = configured ?? DetectedPages.GetOrAdd(shopId ?? string.Empty, Detect);
            return pageId is int id && id > 0 ? PdpPreview.BuildUrl(id, productId, variantId) : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Resolves the shop a product group belongs to, for screens that only know the group.</summary>
    public static string? ShopForGroup(string? groupId, string? languageId)
    {
        if (string.IsNullOrEmpty(groupId))
            return null;

        try
        {
            var language = string.IsNullOrEmpty(languageId)
                ? Dynamicweb.Ecommerce.Services.Languages.GetDefaultLanguageId()
                : languageId;
            return Dynamicweb.Ecommerce.Services.ProductGroups.GetGroup(groupId, language)?.ShopId;
        }
        catch
        {
            return null;
        }
    }

    private static int? Detect(string shopId)
    {
        try
        {
            var areas = Dynamicweb.Content.Services.Areas.GetAreas()
                .Where(a => a.Active)
                .OrderBy(a => a.ID)
                .ToList();

            // Prefer websites bound to the shop; with no shop (or no bound website) any
            // website with a product-detail page still gives a working preview.
            var candidates = string.IsNullOrEmpty(shopId)
                ? areas
                : areas.Where(a => string.Equals(a.EcomShopId, shopId, StringComparison.OrdinalIgnoreCase))
                    .Concat(areas.Where(a => !string.Equals(a.EcomShopId, shopId, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

            foreach (var area in candidates)
            {
                foreach (var itemType in PdpItemTypes)
                {
                    var page = Dynamicweb.Content.Services.Pages.GetPagesByAreaID(area.ID)
                        .FirstOrDefault(p => string.Equals(p.ItemType, itemType, StringComparison.OrdinalIgnoreCase));
                    if (page is not null)
                        return page.ID;
                }
            }
        }
        catch
        {
            // Fall through to "not detectable".
        }

        return null;
    }
}
