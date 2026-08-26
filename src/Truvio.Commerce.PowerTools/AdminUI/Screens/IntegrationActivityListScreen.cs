using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Truvio.Commerce.PowerTools.AdminUI.Security;

namespace Truvio.Commerce.PowerTools.AdminUI.Screens;

/// <summary>
/// Data-integration activities and the tasks that run them. A task pointing at an activity that
/// no longer exists gets its own row, marked Missing.
/// </summary>
public sealed class IntegrationActivityListScreen : ListScreenBase<IntegrationActivityModel>
{
    protected override string GetScreenName() => "Integration activities";

#if DW_HAS_SCREEN_EXPLANATION
    protected override string? GetScreenExplanation() =>
        "Every activity, its providers, its last run, and which task schedules it";
#endif

    protected override IEnumerable<ListViewMapping> GetViewMappings() =>
    [
        new RowViewMapping
        {
            Columns =
            [
                CreateMapping(m => m.Name),
                CreateMapping(m => m.Source),
                CreateMapping(m => m.Destination),
                CreateMapping(m => m.ScheduledBy),
                CreateMapping(m => m.LastRun),
                CreateMapping(m => m.LastResult)
            ]
        }
    ];

    protected override Cell? GetCell(string propertyName, IntegrationActivityModel model) =>
        propertyName == nameof(IntegrationActivityModel.LastResult)
            ? OpsBadges.ActivityResult(model.ResultKind, model.LastResult)
            : null;

    protected override ActionBase? GetListItemPrimaryAction(IntegrationActivityModel model)
    {
        if (!PowerToolsAccess.CanUseOperations() || string.IsNullOrEmpty(model.ActivityId))
            return null;

        return NavigateScreenAction.To<IntegrationActivityDetailScreen>()
            .With(new IntegrationActivityDetailQuery { ActivityId = model.ActivityId });
    }
}
