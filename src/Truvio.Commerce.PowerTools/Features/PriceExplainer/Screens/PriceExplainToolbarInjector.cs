using System.Net;
using Dynamicweb.CoreUI;
using System.Text;
using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Displays.Widgets;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Screens;
using Icon = Dynamicweb.CoreUI.Icons.Icon;
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Dw;
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Queries;
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Selectors;
using Truvio.Commerce.PowerTools.Features.ProductPreview.Dw;
using Truvio.Commerce.PowerTools.Shared.AdminUI;

namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Screens;

/// <summary>
/// Puts the report's context switches in the top bar next to the Actions menu — one button
/// per dimension, labelled with the value in effect (like the Visual Editor's device
/// selector, one hop instead of scanning a long mixed menu). An injector because
/// <c>OverviewScreenBase</c> keeps its <c>ScreenLayout</c> private: <c>AddInManager</c>
/// discovers <c>ScreenInjector&lt;T&gt;</c> subclasses and <c>OnAfter</c> hands over the
/// built layout, whose <c>AddAction</c> is the bar (<c>ContextActionGroups</c> is the
/// Actions dropdown).
/// </summary>
public sealed class PriceExplainToolbarInjector : ScreenInjector<PriceExplainScreen>
{
    public override void OnAfter(PriceExplainScreen screen, UiComponentBase content)
    {
        if (content is not ScreenLayout layout)
            return;

        if (screen.Query is not PriceExplainQuery q || string.IsNullOrEmpty(q.ProductId))
            return;

        var accountToken = Guid.NewGuid().ToString("N");
        ToolbarSwitch.AddPicker(layout, PriceExplainScreen.AccountLabel(q.AccountKey), Icon.UserCircle,
            new AccountSelectorProvider(), accountToken,
            NavigateScreenAction.To<PriceExplainScreen>()
                .With(PriceExplainScreen.Copy(q, x => x.AccountPickToken = accountToken)));

        var productToken = Guid.NewGuid().ToString("N");
        ToolbarSwitch.AddPicker(layout, string.IsNullOrEmpty(q.ProductId) ? "Product" : q.ProductId, Icon.Tag,
            new Selectors.ExplainerProductSelectorProvider(), productToken,
            NavigateScreenAction.To<PriceExplainScreen>()
                .With(PriceExplainScreen.Copy(q, x => x.ProductPickToken = productToken)));

        // One "Context" button labelled with the values in effect. It opens a slide-over with
        // one titled section per dimension, values click-to-apply — the earlier flat
        // split-button menu mixed forty options in one scrolling list whose group titles
        // barely rendered.
        var labelParts = new List<string>();

        var currencies = SafeList(DwCommerceExplainer.Currencies);
        if (currencies.Count > 1 && !string.IsNullOrEmpty(q.CurrencyCode))
            labelParts.Add(q.CurrencyCode);

        var shops = SafeList(DwCommerceExplainer.Shops);
        if (shops.Count > 1)
        {
            var current = shops.FirstOrDefault(shop => shop.Id == q.ShopId);
            if (!string.IsNullOrEmpty(current.Name))
                labelParts.Add(Shorten(current.Name));
        }

        labelParts.Add($"×{q.Quantity:0.##}");
        labelParts.Add(string.IsNullOrEmpty(q.Date) ? "Now" : q.Date);

        ToolbarSwitch.AddButton(layout, string.Join(" · ", labelParts), Icon.SlidersV,
            OpenSlideOverAction.To<PriceContextScreen>().With(new PriceContextQuery
            {
                AccountKey = q.AccountKey,
                ProductId = q.ProductId,
                VariantId = q.VariantId,
                LanguageId = q.LanguageId,
                CurrencyCode = q.CurrencyCode,
                CountryCode = q.CountryCode,
                ShopId = q.ShopId,
                Quantity = q.Quantity,
                Date = q.Date
            }));

        // Opens the storefront PDP in a new tab — the mapped (or auto-detected) product page
        // with the product in context. Renders as the browser's own frontend session.
        var previewUrl = SafePreviewUrl(q);
        if (previewUrl is not null)
            ToolbarSwitch.AddButton(layout, "Preview", Icon.ExternalLinkAlt, NavigateLinkAction.To(previewUrl));
    }

    private static string? SafePreviewUrl(PriceExplainQuery q)
    {
        try
        {
            return DwPdpLocator.UrlFor(q.ShopId, q.ProductId, q.VariantId);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>A shop name short enough for the context label — the panel shows it in full.</summary>
    private static string Shorten(string name) =>
        name.Length <= 16 ? name : name[..15].TrimEnd() + "…";

    private static IReadOnlyList<T> SafeList<T>(Func<IReadOnlyList<T>> source)
    {
        try
        {
            return source();
        }
        catch
        {
            return [];
        }
    }
}
