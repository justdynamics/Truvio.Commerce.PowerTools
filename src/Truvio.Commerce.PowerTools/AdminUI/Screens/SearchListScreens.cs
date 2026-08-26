using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Icons;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Truvio.Commerce.PowerTools.AdminUI.Security;
using Truvio.Commerce.PowerTools.Core.Settings.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Screens;

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

/// <summary>One row per index field, with everything that references it.</summary>
public sealed class FieldUsageScreen : ListScreenBase<FieldUsageModel>
{
    private FieldUsageQuery Q => Query as FieldUsageQuery ?? new FieldUsageQuery();

    protected override string GetScreenName() => "Overview";

#if DW_HAS_SCREEN_EXPLANATION
    protected override string? GetScreenExplanation() =>
        "Search by field name; 'Dangling' means a query references a field the index does not have, " +
        "'Unused' means an indexed field nothing ever asks for";
#endif

    protected override IEnumerable<ActionGroup>? GetScreenActions() =>
    [
        new ActionGroup
        {
            Nodes =
            [
                new ActionNode
                {
                    Name = "Repositories & indexes",
                    Icon = Icon.Database,
                    NodeAction = NavigateScreenAction.To<IndexListScreen>().With(new IndexListQuery())
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
                // Status first, like the Warnings screen: field system names are long and
                // push the trailing columns out of view.
                CreateMapping(m => m.Status),
                CreateMapping(m => m.Field),
                CreateMapping(m => m.Index),
                CreateMapping(m => m.Type),
                CreateMapping(m => m.UsedBy)
            ]
        }
    ];

    protected override Cell? GetCell(string propertyName, FieldUsageModel model) =>
        propertyName == nameof(FieldUsageModel.Status)
            ? SearchBadges.FieldStatus(model.StatusKind)
            : null;
}

/// <summary>Lint findings across every query and facet group in the install.</summary>
public sealed class QueryLintScreen : ListScreenBase<QueryLintModel>
{
    protected override string GetScreenName() => "Findings";

#if DW_HAS_SCREEN_EXPLANATION
    protected override string? GetScreenExplanation() =>
        "Rules IDX-W1..IDX-W17 over every query, sort, facet and index in the repositories";
#endif

    protected override IEnumerable<ActionGroup>? GetScreenActions() =>
    [
        new()
        {
            Nodes =
            [
                new ActionNode
                {
                    Name = "Repositories & indexes",
                    Icon = Icon.Database,
                    NodeAction = NavigateScreenAction.To<IndexListScreen>().With(new IndexListQuery())
                },
                new ActionNode
                {
                    Name = "PowerTools settings",
                    Icon = Icon.Cog,
                    NodeAction = NavigateScreenAction.To<PowerToolsSettingsScreen>().With(new PowerToolsSettingsQuery())
                }
            ]
        }
    ];

    protected override IEnumerable<ListViewMapping> GetViewMappings()
    {
        var columns = new List<Dynamicweb.CoreUI.Data.ModelMapping> { CreateMapping(m => m.Severity) };
        if (DwPowerToolsSettings.Current.ShowRuleIds)
            columns.Add(CreateMapping(m => m.RuleId));
        columns.Add(CreateMapping(m => m.Entity));
        columns.Add(CreateMapping(m => m.Title));
        columns.Add(CreateMapping(m => m.Detail));

        return [new RowViewMapping { Columns = columns }];
    }

    // The trailing "N findings hidden by settings" row has no severity: no badge for it.
    protected override Cell? GetCell(string propertyName, QueryLintModel model) =>
        propertyName == nameof(QueryLintModel.Severity) && !string.IsNullOrEmpty(model.Severity)
            ? Badges.Severity(model.Severity)
            : null;
}

/// <summary>Step 1 of the document browser: pick the index to read.</summary>
public sealed class IndexPickScreen : ListScreenBase<IndexPickModel>
{
    protected override string GetScreenName() => "Document browser - indexes";

#if DW_HAS_SCREEN_EXPLANATION
    protected override string? GetScreenExplanation() =>
        "Pick an index to read its documents; an index that has never been built has nothing to show";
#endif

    protected override IEnumerable<ListViewMapping> GetViewMappings() =>
    [
        new RowViewMapping
        {
            Columns =
            [
                CreateMapping(m => m.Repository),
                CreateMapping(m => m.Index),
                CreateMapping(m => m.Instance),
                CreateMapping(m => m.Documents),
                CreateMapping(m => m.Status)
            ]
        }
    ];

    protected override Cell? GetCell(string propertyName, IndexPickModel model) =>
        propertyName == nameof(IndexPickModel.Status)
            ? SearchBadges.Health(model.HealthKind, model.Status)
            : null;

    protected override ActionBase? GetListItemPrimaryAction(IndexPickModel model) =>
        PowerToolsAccess.CanUseSearchInspector()
            ? NavigateScreenAction.To<DocumentBrowserScreen>()
                .With(new DocumentBrowserQuery { Repository = model.RepositoryName, Item = model.Item })
            : null;
}


/// <summary>The dangling/unused filter as a toolbar control labelled with the view in effect.</summary>
public sealed class FieldUsageToolbarInjector : ScreenInjector<FieldUsageScreen>
{
    public override void OnAfter(FieldUsageScreen screen, Dynamicweb.CoreUI.UiComponentBase content)
    {
        if (content is not Dynamicweb.CoreUI.Layout.ScreenLayout layout)
            return;

        var problemsOnly = (screen.Query as FieldUsageQuery)?.ProblemsOnly ?? false;

        ToolbarSwitch.Add(layout, problemsOnly ? "Problems only" : "All fields", Icon.Filter,
        [
            ToolbarSwitch.Option("All fields", active: !problemsOnly,
                NavigateScreenAction.To<FieldUsageScreen>().With(new FieldUsageQuery())),
            ToolbarSwitch.Option("Only dangling and unused", active: problemsOnly,
                NavigateScreenAction.To<FieldUsageScreen>().With(new FieldUsageQuery { ProblemsOnly = true }))
        ]);
    }
}
