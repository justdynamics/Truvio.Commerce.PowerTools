using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Truvio.Commerce.PowerTools.AdminUI.Security;

namespace Truvio.Commerce.PowerTools.AdminUI.Screens;

/// <summary>Every scheduled task, with the state the install actually reports.</summary>
public sealed class ScheduledTaskListScreen : ListScreenBase<ScheduledTaskModel>
{
    protected override string GetScreenName() => "Scheduled tasks";

#if DW_HAS_SCREEN_EXPLANATION
    protected override string? GetScreenExplanation() =>
        "What runs, when it last ran, and what happened; pick a task for its run history";
#endif

    protected override IEnumerable<ListViewMapping> GetViewMappings() =>
    [
        new RowViewMapping
        {
            Columns =
            [
                CreateMapping(m => m.Name),
                CreateMapping(m => m.AddIn),
                CreateMapping(m => m.Schedule),
                CreateMapping(m => m.Status),
                CreateMapping(m => m.LastRun),
                CreateMapping(m => m.NextRun)
            ]
        }
    ];

    protected override Cell? GetCell(string propertyName, ScheduledTaskModel model) =>
        propertyName == nameof(ScheduledTaskModel.Status)
            ? OpsBadges.TaskState(model.State, model.Status)
            : null;

    protected override ActionBase? GetListItemPrimaryAction(ScheduledTaskModel model)
    {
        if (!PowerToolsAccess.CanUseOperations() || model.TaskId <= 0)
            return null;

        return NavigateScreenAction.To<ScheduledTaskDetailScreen>()
            .With(new ScheduledTaskDetailQuery { TaskId = model.TaskId });
    }
}
