using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.Features.QueryTester.Models;

namespace Truvio.Commerce.PowerTools.Features.QueryTester.Screens;

/// <summary>
/// The "Why 'X'?" panel, opened as a slide-over from a Documents row — the same clause-probe
/// table the report shows for <c>#expect</c>, without leaving the report.
/// </summary>
public sealed class QueryWhyScreen : OverviewScreenBase<QueryWhyModel>
{
    protected override string GetScreenName() => Model?.Heading ?? "Why?";

    protected override void BuildOverviewScreen()
    {
        if (Model is null)
            return;

        // The heading is already the screen name - an unnamed group avoids showing it twice.
        AddComponent(new HtmlBlock { Value = Model.Html }, string.Empty, Group.GroupWidth.Col_12);
    }
}
