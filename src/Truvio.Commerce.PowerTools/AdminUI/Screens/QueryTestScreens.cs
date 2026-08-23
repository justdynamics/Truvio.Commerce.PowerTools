using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Displays.Widgets;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
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
/// Step 2, optional: the values for the run. An overview screen cannot take form input, so the
/// toolbar search box doubles as the input — type <c>name=value</c> and press Enter. The
/// accumulated set travels on in the screen URL.
/// </summary>
public sealed class QueryParameterScreen : ListScreenBase<QueryParameterModel>
{
    private QueryParameterQuery Q => Query as QueryParameterQuery ?? new QueryParameterQuery();

    protected override string GetScreenName() => "Set parameters";

#if DW_HAS_SCREEN_EXPLANATION
    protected override string? GetScreenExplanation() =>
        "Type name=value in the search box and press Enter to set a value; type several as name=a;other=b. Text without '=' just filters this list";
#endif

    protected override IEnumerable<ListViewMapping> GetViewMappings() =>
    [
        new RowViewMapping
        {
            Columns =
            [
                CreateMapping(m => m.Name),
                CreateMapping(m => m.Type),
                CreateMapping(m => m.Default),
                CreateMapping(m => m.Value),
                CreateMapping(m => m.Effect)
            ]
        }
    ];

    protected override Cell? GetCell(string propertyName, QueryParameterModel model) =>
        propertyName == nameof(QueryParameterModel.Value) && model.StateKind is "set" or "blank"
            ? Cell.MakeCell(new Badge
            {
                Value = model.Value,
                BadgeType = model.StateKind == "set" ? BadgeType.Success : BadgeType.Warning
            })
            : null;

    /// <summary>Clicking a parameter clears it — the only single-click edit a list row can express.</summary>
    protected override ActionBase? GetListItemPrimaryAction(QueryParameterModel model)
    {
        if (!PowerToolsAccess.CanUseSearchInspector())
            return null;

        var q = Q;
        return NavigateScreenAction.To<QueryParameterScreen>()
            .With(new QueryParameterQuery
            {
                Repository = q.Repository,
                Item = q.Item,
                Parameters = ParameterValues.Set(q.Parameters, model.ParameterName, null)
            });
    }

    protected override IEnumerable<ActionGroup>? GetScreenActions()
    {
        var q = Q;

        return
        [
            new ActionGroup
            {
                Nodes =
                [
                    new ActionNode
                    {
                        Name = "Run the query",
                        Icon = Icon.Play,
                        NodeAction = NavigateScreenAction.To<QueryTestScreen>()
                            .With(new QueryTestQuery { Repository = q.Repository, Item = q.Item, Parameters = q.Parameters })
                    }
                ]
            },
            new ActionGroup
            {
                Nodes =
                [
                    new ActionNode
                    {
                        Name = "Use the declared defaults",
                        Icon = Icon.Redo,
                        NodeAction = NavigateScreenAction.To<QueryParameterScreen>()
                            .With(new QueryParameterQuery
                            {
                                Repository = q.Repository,
                                Item = q.Item,
                                Parameters = Defaults(q)
                            })
                    },
                    new ActionNode
                    {
                        Name = "Clear all values",
                        Icon = Icon.TrashAlt,
                        NodeAction = NavigateScreenAction.To<QueryParameterScreen>()
                            .With(new QueryParameterQuery { Repository = q.Repository, Item = q.Item })
                    }
                ]
            },
            new ActionGroup
            {
                Nodes =
                [
                    new ActionNode
                    {
                        Name = "Select another query",
                        Icon = Icon.ListUl,
                        NodeAction = NavigateScreenAction.To<QueryPickScreen>().With(new QueryPickQuery())
                    }
                ]
            }
        ];
    }

    private static string Defaults(QueryParameterQuery q)
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
/// Step 3: the report. An overview screen, because every section is long text — the list grid
/// gives all columns the same width and clips. Every state (values, result size, whether the
/// per-clause impact was measured) is in the URL, so a finding can be shared as a link.
/// </summary>
public sealed class QueryTestScreen : OverviewScreenBase<QueryTestModel>
{
    private static readonly int[] TakePresets = [10, 25];

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
                        Name = "Set parameters",
                        Icon = Icon.SlidersV,
                        NodeAction = NavigateScreenAction.To<QueryParameterScreen>()
                            .With(new QueryParameterQuery
                            {
                                Repository = q.Repository,
                                Item = q.Item,
                                Parameters = q.Parameters
                            })
                    },
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
                Nodes = TakePresets.Select(take => new ActionNode
                {
                    Name = $"Show {take} documents",
                    Icon = Icon.ListUl,
                    NodeAction = Navigate(q, x => x.Take = take)
                }).ToList()
            },
            new()
            {
                Nodes =
                [
                    new ActionNode
                    {
                        Name = q.Impact ? "Skip the per-clause impact" : "Measure the per-clause impact",
                        Icon = Icon.Comparison,
                        NodeAction = Navigate(q, x => x.Impact = !q.Impact)
                    },
                    new ActionNode
                    {
                        Name = q.ShowFacets ? "Hide facet counts" : "Show facet counts",
                        Icon = Icon.ChartBar,
                        NodeAction = Navigate(q, x => x.ShowFacets = !q.ShowFacets)
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

    private static NavigateScreenAction Navigate(QueryTestQuery q, Action<QueryTestQuery> change)
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
