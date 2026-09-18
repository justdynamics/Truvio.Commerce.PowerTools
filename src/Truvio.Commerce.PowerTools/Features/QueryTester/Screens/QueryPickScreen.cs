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
using Truvio.Commerce.PowerTools.Features.IndexInspector.Queries;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Screens;
using Truvio.Commerce.PowerTools.Features.QueryTester.Models;
using Truvio.Commerce.PowerTools.Features.QueryTester.Queries;
using Truvio.Commerce.PowerTools.Shared.Security;

namespace Truvio.Commerce.PowerTools.Features.QueryTester.Screens;

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
