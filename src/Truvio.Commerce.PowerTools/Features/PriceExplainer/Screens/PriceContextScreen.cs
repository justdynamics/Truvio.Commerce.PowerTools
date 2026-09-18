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
