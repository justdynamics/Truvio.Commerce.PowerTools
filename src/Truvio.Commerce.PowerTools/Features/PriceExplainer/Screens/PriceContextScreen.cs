using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Models;

namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Screens;

/// <summary>The context slide-over: pick a currency, shop, quantity or date to re-explain with.</summary>
public sealed class PriceContextScreen : OverviewScreenBase<PriceContextModel>
{
    protected override string GetScreenName() => Model?.Heading ?? "Context";

    protected override void BuildOverviewScreen()
    {
        if (Model is null)
            return;

        // The heading is already the screen name — an unnamed group avoids showing it twice.
        AddComponent(new HtmlBlock { Value = Model.Html }, string.Empty, Group.GroupWidth.Col_12);
    }
}
