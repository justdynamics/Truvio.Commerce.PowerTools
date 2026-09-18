using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Displays.Widgets;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Screens;
using Icon = Dynamicweb.CoreUI.Icons.Icon;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Queries;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Screens;
using Truvio.Commerce.PowerTools.Features.QueryTester.Core;
using Truvio.Commerce.PowerTools.Features.QueryTester.Models;
using Truvio.Commerce.PowerTools.Features.QueryTester.Queries;

namespace Truvio.Commerce.PowerTools.Features.QueryTester.Screens;

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
        Model is null || string.IsNullOrEmpty(Model.QueryName) ? "Query tester" : Model.QueryName;

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
