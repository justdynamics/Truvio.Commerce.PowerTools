using System.Globalization;
using System.Net;

namespace Truvio.Commerce.PowerTools.Core.Commerce;

/// <summary>
/// Resolves where "Preview in shop" navigates: the admin maps shops to product-detail pages in
/// PowerTools settings ("SHOP40=1234" per line, a bare page id as the default for every other
/// shop), and the URL form <c>/Default.aspx?ID={page}&amp;ProductID={id}</c> is DW's own
/// always-valid entry — the frontend 301-redirects it to the friendly URL and renders the
/// product (verified live). Pure and host-free; the DW-facing lookup lives in
/// <c>Dw/DwPdpLocator</c>.
/// </summary>
public static class PdpPreview
{
    /// <summary>
    /// The page configured for <paramref name="shopId"/>: an explicit "SHOP=page" line wins,
    /// then a bare page-id line as the default. Null when nothing is configured — the caller
    /// falls back to auto-detection or hides the button.
    /// </summary>
    public static int? ResolvePageId(string? previewPages, string? shopId)
    {
        if (string.IsNullOrWhiteSpace(previewPages))
            return null;

        int? fallback = null;
        foreach (var raw in previewPages.Split(['\n', '\r', ',', ';'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (line.Length == 0)
                continue;

            var separator = line.IndexOf('=');
            if (separator < 0)
            {
                // A bare page id is the default for every shop without an explicit line.
                if (fallback is null && int.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id > 0)
                    fallback = id;
                continue;
            }

            var shop = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (shop.Length == 0 || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pageId) || pageId <= 0)
                continue;

            if (!string.IsNullOrEmpty(shopId) && shop.Equals(shopId, StringComparison.OrdinalIgnoreCase))
                return pageId;
        }

        return fallback;
    }

    /// <summary>The always-valid DW entry URL; the frontend rewrites it to the friendly URL.</summary>
    public static string BuildUrl(int pageId, string productId, string? variantId = null)
    {
        var url = $"/Default.aspx?ID={pageId}&ProductID={WebUtility.UrlEncode(productId)}";
        return string.IsNullOrEmpty(variantId) ? url : $"{url}&VariantID={WebUtility.UrlEncode(variantId)}";
    }
}
