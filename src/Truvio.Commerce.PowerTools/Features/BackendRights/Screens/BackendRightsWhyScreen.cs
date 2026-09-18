using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.Features.BackendRights.Models;

namespace Truvio.Commerce.PowerTools.Features.BackendRights.Screens;

/// <summary>The "Why?" slide-over: one area, section or node explained in full.</summary>
public sealed class BackendRightsWhyScreen : OverviewScreenBase<BackendRightsWhyModel>
{
    protected override string GetScreenName() => Model?.Heading ?? "Why?";

    protected override void BuildOverviewScreen()
    {
        if (Model is null)
            return;

        // The heading is already the screen name — an unnamed group avoids showing it twice.
        AddComponent(new HtmlBlock { Value = Model.Html }, string.Empty, Group.GroupWidth.Col_12);
    }
}
