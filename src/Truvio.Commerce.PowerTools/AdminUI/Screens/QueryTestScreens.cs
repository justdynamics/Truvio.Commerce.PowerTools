using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Displays.Widgets;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Data;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.AdminUI.Commands;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Truvio.Commerce.PowerTools.AdminUI.Security;
using Truvio.Commerce.PowerTools.Core.Search;
using Truvio.Commerce.PowerTools.Core.Search.Testing;
using Icon = Dynamicweb.CoreUI.Icons.Icon;

namespace Truvio.Commerce.PowerTools.AdminUI.Screens;

/// <summary>Step 1 of the Query tester: which query do you want to run?</summary>
public sealed class QueryPickScreen : ListScreenBase<QueryPickModel>
{
    protected override string GetScreenName() => "Query tester";

#if DW_HAS_SCREEN_EXPLANATION
    protected override string? GetScreenExplanation() =>
        "Pick a query to run it against its live index and see why the result is what it is";
#endif

    protected override IEnumerable<ListViewMapping> GetViewMappings() =>
    [
        new RowViewMapping
        {
            Columns =
            [
                CreateMapping(m => m.Repository),
                CreateMapping(m => m.Query),
                CreateMapping(m => m.Source),
                CreateMapping(m => m.Parameters),
                CreateMapping(m => m.Status)
            ]
        }
    ];

    protected override Cell? GetCell(string propertyName, QueryPickModel model) =>
        propertyName == nameof(QueryPickModel.Status)
            ? Cell.MakeCell(new Badge
            {
                Value = model.Status,
                BadgeType = model.HealthKind switch
                {
                    "Ok" => BadgeType.Success,
                    "Stale" => BadgeType.Warning,
                    _ => BadgeType.Danger
                }
            })
            : null;

    protected override ActionBase? GetListItemPrimaryAction(QueryPickModel model)
    {
        if (!PowerToolsAccess.CanUseSearchInspector())
            return null;

        return NavigateScreenAction.To<QueryTestScreen>()
            .With(new QueryTestQuery { Repository = model.RepositoryName, Item = model.Item });
    }

    protected override IEnumerable<ActionGroup>? GetScreenActions() =>
    [
        new ActionGroup
        {
            Nodes =
            [
                new ActionNode
                {
                    Name = "Query linter",
                    Icon = Icon.Bug,
                    NodeAction = NavigateScreenAction.To<QueryLintScreen>().With(new QueryLintQuery())
                },
                new ActionNode
                {
                    Name = "Repositories & indexes",
                    Icon = Icon.Database,
                    NodeAction = NavigateScreenAction.To<IndexListScreen>().With(new IndexListQuery())
                }
            ]
        }
    ];
}

/// <summary>
/// Step 2, optional: the values for the run — a dialog with one text input per declared
/// parameter (a prompt screen, the only CoreUI screen kind whose OK posts edited values back
/// to a command). OK saves the set as the user's draft and opens the report with
/// <c>UseDraft=true</c>; the report then renders every link with the resolved values, so
/// shareability is kept.
/// </summary>
public sealed class QueryValuesScreen : PromptScreenBase<QueryValuesModel>
{
    private QueryValuesQuery Q => Query as QueryValuesQuery ?? new QueryValuesQuery();

    protected override string GetScreenName() =>
        string.IsNullOrEmpty(Model?.QueryName) ? "Set parameters" : $"Set parameters: {Model.QueryName}";

    protected override void BuildPromptScreen() => AddDynamicFields(m => m.Fields);

    protected override string GetOkActionName() => "Run the query";

    /// <summary>Null when the user may not run queries: CoreUI then renders no OK button.</summary>
    protected override CommandBase<QueryValuesModel>? GetOkCommand() =>
        PowerToolsAccess.CanUseSearchInspector()
            ? new QueryValuesRunCommand { Repository = Q.Repository, Item = Q.Item }
            : null;

    /// <summary>
    /// OK saves the draft, then opens the report reading it. ForceReload matters: rerunning
    /// from a report that is already at the <c>UseDraft</c> URL navigates to the same URL,
    /// which the client otherwise treats as a no-op and the old report stays on screen.
    /// </summary>
    protected override RunCommandAction ConfigureOkAction(RunCommandAction action) =>
        action.WithOnSuccess(NavigateScreenAction.To<QueryTestScreen>()
            .With(new QueryTestQuery { Repository = Q.Repository, Item = Q.Item, UseDraft = true })
            .WithForceReload()
            .UpdateParameters(p => p.Replace = true));
}

/// <summary>
/// Step 3: the report. An overview screen, because every section is long text — the list grid
/// gives all columns the same width and clips. Every state (values, result size, whether the
/// per-clause impact was measured) is in the URL, so a finding can be shared as a link.
/// </summary>
public sealed class QueryTestScreen : OverviewScreenBase<QueryTestModel>
{
    internal static readonly int[] TakePresets = [10, 25];

    private QueryTestQuery Q => Query as QueryTestQuery ?? new QueryTestQuery();

    protected override string GetScreenName() =>
        Model is null || string.IsNullOrEmpty(Model.QueryName) ? "Query tester" : $"Query tester: {Model.QueryName}";

    protected override void BuildOverviewScreen()
    {
        var model = Model;
        if (model is null)
            return;

        if (!string.IsNullOrEmpty(model.Error))
        {
            AddComponent(new Alert { Value = model.Error, Icon = Icon.ExclamationTriangle }, "The query could not be tested", Group.GroupWidth.Col_12);
            return;
        }

        SetInfobar(new InfoBar
        {
            Icon = Icon.Flask,
            Information = new Dictionary<string, CardInfo.InfoValue>
            {
                ["Index"] = new(model.IndexName),
                ["Instance"] = new(string.IsNullOrEmpty(model.Instance) ? "-" : model.Instance),
                ["Hits"] = new(model.Hits),
                ["Took"] = new(model.Took),
                ["Verdict"] = new(new Badge
                {
                    Value = model.Verdict,
                    BadgeType = model.VerdictKind switch
                    {
                        "ok" => BadgeType.Success,
                        "warn" => BadgeType.Warning,
                        _ => BadgeType.Danger
                    }
                })
            }
        });

        foreach (var section in model.Sections)
            AddComponent(new HtmlBlock { Value = section.Html }, section.Heading, Group.GroupWidth.Col_12);
    }

    protected override IEnumerable<ActionGroup>? GetScreenActions()
    {
        var q = Q;

        var groups = new List<ActionGroup>
        {
            new()
            {
                Nodes =
                [
                    new ActionNode
                    {
                        Name = "Use the declared defaults",
                        Icon = Icon.Redo,
                        NodeAction = Navigate(q, x => x.Parameters = Defaults(q))
                    },
                    new ActionNode
                    {
                        Name = "Clear all values",
                        Icon = Icon.TrashAlt,
                        NodeAction = Navigate(q, x => x.Parameters = string.Empty)
                    }
                ]
            },
            new()
            {
                Nodes =
                [
                    new ActionNode
                    {
                        Name = "Select another query",
                        Icon = Icon.ListUl,
                        NodeAction = NavigateScreenAction.To<QueryPickScreen>().With(new QueryPickQuery())
                    },
                    new ActionNode
                    {
                        Name = "Index detail",
                        Icon = Icon.Database,
                        NodeAction = IndexDetail(q)
                    }
                ]
            }
        };

        return groups;
    }

    internal static NavigateScreenAction Navigate(QueryTestQuery q, Action<QueryTestQuery> change)
    {
        var next = new QueryTestQuery
        {
            Repository = q.Repository,
            Item = q.Item,
            Parameters = q.Parameters,
            Take = q.Take,
            Impact = q.Impact,
            ShowFacets = q.ShowFacets
        };
        change(next);
        return NavigateScreenAction.To<QueryTestScreen>().With(next);
    }

    private static NavigateScreenAction IndexDetail(QueryTestQuery q)
    {
        var source = Source(q);
        return NavigateScreenAction.To<IndexDetailScreen>()
            .With(new IndexDetailQuery { Repository = source.Repository, Item = source.Item });
    }

    private static (string Repository, string Item) Source(QueryTestQuery q)
    {
        try
        {
            var query = SearchQueryHelpers.Catalog().Query(q.Repository, q.Item);
            return query is null ? (q.Repository, q.Item) : (query.SourceRepository, query.SourceItem);
        }
        catch
        {
            return (q.Repository, q.Item);
        }
    }

    private static string Defaults(QueryTestQuery q)
    {
        try
        {
            var query = SearchQueryHelpers.Catalog().Query(q.Repository, q.Item);
            return query is null ? string.Empty : ParameterValues.Defaults(query);
        }
        catch
        {
            return string.Empty;
        }
    }
}


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
