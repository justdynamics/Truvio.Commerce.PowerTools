using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Icons;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Models;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Queries;
using Truvio.Commerce.PowerTools.Features.Settings.Dw;
using Truvio.Commerce.PowerTools.Features.Settings.Queries;
using Truvio.Commerce.PowerTools.Features.Settings.Screens;
using Truvio.Commerce.PowerTools.Shared.AdminUI;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Screens;

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
