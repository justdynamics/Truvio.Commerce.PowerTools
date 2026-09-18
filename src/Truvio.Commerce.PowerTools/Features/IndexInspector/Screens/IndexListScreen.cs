using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Icons;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Models;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Queries;
using Truvio.Commerce.PowerTools.Shared.Security;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Screens;

/// <summary>Every index in every repository, with its build health.</summary>
public sealed class IndexListScreen : ListScreenBase<IndexListModel>
{
    protected override string GetScreenName() => "Overview";

#if DW_HAS_SCREEN_EXPLANATION
    protected override string? GetScreenExplanation() =>
        "Pick an index to see its schema, instances, builder settings and the queries that read from it";
#endif

    protected override IEnumerable<ActionGroup>? GetScreenActions() =>
    [
        new()
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
                    Name = "Field where-used",
                    Icon = Icon.Sitemap,
                    NodeAction = NavigateScreenAction.To<FieldUsageScreen>().With(new FieldUsageQuery())
                }
            ]
        }
    ];

    protected override IEnumerable<ListViewMapping> GetViewMappings() =>
    [
        new RowViewMapping
        {
            Columns =
            [
                CreateMapping(m => m.Repository),
                CreateMapping(m => m.Index),
                CreateMapping(m => m.Builder),
                CreateMapping(m => m.Fields),
                CreateMapping(m => m.LastBuild),
                CreateMapping(m => m.Status)
            ]
        }
    ];

    protected override Cell? GetCell(string propertyName, IndexListModel model) =>
        propertyName == nameof(IndexListModel.Status)
            ? SearchBadges.Health(model.HealthKind, model.Status)
            : null;

    protected override ActionBase? GetListItemPrimaryAction(IndexListModel model) =>
        PowerToolsAccess.CanUseSearchInspector()
            ? NavigateScreenAction.To<IndexDetailScreen>()
                .With(new IndexDetailQuery { Repository = model.RepositoryName, Item = model.Item })
            : null;
}
