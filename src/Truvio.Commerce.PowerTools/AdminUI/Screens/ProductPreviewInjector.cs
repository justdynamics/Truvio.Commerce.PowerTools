using Dynamicweb.CoreUI;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Screens;
using Dynamicweb.Products.UI.Queries;
using Icon = Dynamicweb.CoreUI.Icons.Icon;

namespace Truvio.Commerce.PowerTools.AdminUI.Screens;

/// <summary>
/// Puts "Preview in shop" in the toolbar of DW's own product edit screen — the PIM editor's
/// natural place to see the storefront result of what was just enriched. Server-side via
/// <c>ScreenInjector</c>, so the AdminUI JS-injection limitation (#437) does not apply.
/// Everything is guarded: a failure to resolve the preview page, or any surprise in the host
/// screen's layout, must never affect DW's product editor — the button is simply absent.
/// </summary>
public sealed class ProductPreviewInjector : ScreenInjector<Dynamicweb.Products.UI.Screens.ProductEditScreen>
{
    public override void OnAfter(Dynamicweb.Products.UI.Screens.ProductEditScreen screen, UiComponentBase content)
    {
        try
        {
            if (content is not ScreenLayout layout)
                return;

            if (screen.Query is not ProductByIdQuery q || string.IsNullOrEmpty(q.Id))
                return;

            var shopId = Core.Commerce.Dw.DwPdpLocator.ShopForGroup(q.GroupId, q.LanguageId);
            var url = Core.Commerce.Dw.DwPdpLocator.UrlFor(shopId, q.Id, q.VariantId);
            if (url is null)
                return;

            ToolbarSwitch.AddButton(layout, "Preview in shop", Icon.ExternalLinkAlt, NavigateLinkAction.To(url));
        }
        catch
        {
            // Never let a preview convenience break DW's own screen.
        }
    }
}
