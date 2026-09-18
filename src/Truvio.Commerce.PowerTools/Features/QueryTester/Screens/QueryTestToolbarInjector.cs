using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Displays.Widgets;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Data;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Icon = Dynamicweb.CoreUI.Icons.Icon;
using Truvio.Commerce.PowerTools.Features.QueryTester.Queries;
using Truvio.Commerce.PowerTools.Shared.AdminUI;

namespace Truvio.Commerce.PowerTools.Features.QueryTester.Screens;

/// <summary>
/// The report's run switches as toolbar controls: *Set parameters* as a one-click button
/// (the tool's primary action), then the document count, the per-clause impact and the facet
/// counts as value-labelled selectors — same treatment as the Price Explainer's context
/// switches, see <see cref="ToolbarSwitch"/>.
/// </summary>
public sealed class QueryTestToolbarInjector : ScreenInjector<QueryTestScreen>
{
    public override void OnAfter(QueryTestScreen screen, Dynamicweb.CoreUI.UiComponentBase content)
    {
        if (content is not ScreenLayout layout)
            return;

        if (screen.Query is not QueryTestQuery q || string.IsNullOrEmpty(q.Repository) || string.IsNullOrEmpty(q.Item))
            return;

        ToolbarSwitch.AddButton(layout, "Set parameters", Icon.SlidersV,
            OpenDialogAction.To<QueryValuesScreen>()
                .With(new QueryValuesQuery { Repository = q.Repository, Item = q.Item, Parameters = q.Parameters }));

        ToolbarSwitch.Add(layout, $"{q.Take} docs", Icon.ListUl,
            QueryTestScreen.TakePresets.Select(take => ToolbarSwitch.Option($"Show {take} documents",
                active: take == q.Take, QueryTestScreen.Navigate(q, x => x.Take = take))));

        ToolbarSwitch.Add(layout, q.Impact ? "Impact on" : "Impact off", Icon.Comparison,
        [
            ToolbarSwitch.Option("Measure the per-clause impact", active: q.Impact, QueryTestScreen.Navigate(q, x => x.Impact = true)),
            ToolbarSwitch.Option("Skip the per-clause impact", active: !q.Impact, QueryTestScreen.Navigate(q, x => x.Impact = false))
        ]);

        ToolbarSwitch.Add(layout, q.ShowFacets ? "Facets on" : "Facets off", Icon.ChartBar,
        [
            ToolbarSwitch.Option("Show facet counts", active: q.ShowFacets, QueryTestScreen.Navigate(q, x => x.ShowFacets = true)),
            ToolbarSwitch.Option("Hide facet counts", active: !q.ShowFacets, QueryTestScreen.Navigate(q, x => x.ShowFacets = false))
        ]);
    }
}
