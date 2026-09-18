using Dynamicweb.CoreUI;
using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Displays.Widgets;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Icon = Dynamicweb.CoreUI.Icons.Icon;
using Truvio.Commerce.PowerTools.Features.PimQuality.Models;
using Truvio.Commerce.PowerTools.Features.PimQuality.Queries;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Screens;

/// <summary>
/// Screen 4 — governance: which completion rules govern anything, and which workflows the
/// catalog references. Stuck-state ageing is deliberately absent — DW exposes no read API for
/// how long a product has sat in a workflow state.
/// </summary>
public sealed class PimGovernanceScreen : ListScreenBase<PimGovernanceModel>
{
    protected override string GetScreenName() => "Overview";

#if DW_HAS_SCREEN_EXPLANATION
    protected override string? GetScreenExplanation() =>
        "Completion rules with what they are assigned to, and the workflows product groups and products reference — 'Dead' means the rule scores nothing";
#endif

    protected override IEnumerable<ActionGroup>? GetScreenActions() =>
    [
        new()
        {
            Nodes =
            [
                new ActionNode
                {
                    Name = "Catalog quality",
                    Icon = Icon.Heartbeat,
                    NodeAction = NavigateScreenAction.To<PimQualityScreen>().With(new PimQualityQuery())
                },
                new ActionNode
                {
                    Name = "Completeness explorer",
                    Icon = Icon.Tag,
                    NodeAction = NavigateScreenAction.To<PimCompletenessScreen>().With(new PimCompletenessQuery())
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
                CreateMapping(m => m.Status),
                CreateMapping(m => m.Kind),
                CreateMapping(m => m.Name),
                CreateMapping(m => m.AppliesTo),
                CreateMapping(m => m.Fields)
            ]
        }
    ];

    protected override Cell? GetCell(string propertyName, PimGovernanceModel model) =>
        propertyName == nameof(PimGovernanceModel.Status)
            ? PimBadges.State(model.State, model.Status)
            : null;
}
