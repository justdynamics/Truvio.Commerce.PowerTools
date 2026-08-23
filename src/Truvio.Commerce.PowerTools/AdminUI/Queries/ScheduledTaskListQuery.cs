using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Operations;
using Truvio.Commerce.PowerTools.Core.Operations.Dw;
using Truvio.Commerce.PowerTools.Core.Operations.Rules;
using Truvio.Commerce.PowerTools.Core.Settings.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// Every scheduled task with the state the install actually reports: DW's own last result, plus
/// the stale/never-run verdicts the rules derive. Search matches the task name and the add-in.
/// </summary>
public sealed class ScheduledTaskListQuery : DataQueryListBase<ScheduledTaskModel, ScheduledTaskModel, DataListViewModel<ScheduledTaskModel>>
{
    protected override IEnumerable<ScheduledTaskModel>? GetListItems()
    {
        var source = new DwOperationsSource();
        var now = DateTime.Now;
        var tasks = source.GetTasks();

        // Only the task-shaped rules matter here, and they need no storage read.
        var snapshot = new OperationsSnapshot(tasks, source.GetActivities(), [], [], source.GetRetention(), now);
        var findings = new OperationsHealthEngine([new FailingTaskRule(), OperationsHealthEngine.StaleTask(DwPowerToolsSettings.Current), new BrokenActivityLinkRule()])
            .Run(snapshot)
            .Where(f => f.EntityName == OperationsEntities.ScheduledTask)
            .ToLookup(f => f.EntityKey, StringComparer.Ordinal);

        var search = (Search ?? string.Empty).Trim();

        return tasks
            .Where(t => search.Length == 0
                     || t.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                     || t.AddInShortName.Contains(search, StringComparison.OrdinalIgnoreCase))
            .Select(task =>
            {
                var taskFindings = findings[task.Id.ToString()].ToList();
                var (state, status) = Describe(task, taskFindings, now);

                return new ScheduledTaskModel
                {
                    TaskId = task.Id,
                    State = state,
                    Name = task.Name,
                    AddIn = task.AddInShortName,
                    Schedule = task.Enabled ? OpsFormat.Interval(task.IntervalMinutes) : "disabled",
                    Status = status,
                    LastRun = task.LastRun is null ? "never" : OpsFormat.Relative(task.LastRun, now),
                    NextRun = task.NextRun is null ? "-" : OpsFormat.Relative(task.NextRun, now)
                };
            })
            .ToList();
    }

    /// <summary>
    /// The single word that goes in the badge. Worst-first: a failing task outranks a stale one,
    /// and a disabled task is never called stale.
    /// </summary>
    internal static (string State, string Status) Describe(
        TaskSpec task,
        IReadOnlyList<Core.Diagnostics.Finding> taskFindings,
        DateTime now)
    {
        if (!task.Enabled)
            return ("disabled", "Disabled");

        if (taskFindings.Any(f => f.RuleId == FailingTaskRule.Id))
            return ("failed", "Last run failed");

        if (taskFindings.Any(f => f.RuleId == BrokenActivityLinkRule.BrokenId))
            return ("failed", "Missing activity");

        if (taskFindings.Any(f => f.RuleId == StaleTaskRule.NeverRunId))
            return ("stale", "Never run");

        if (taskFindings.Any(f => f.RuleId == StaleTaskRule.StaleId))
            return ("stale", $"Stale ({OpsFormat.Relative(task.LastRun, now)})");

        return task.LastRun is null ? ("idle", "Waiting") : ("ok", "OK");
    }

    protected override IEnumerable<ScheduledTaskModel> MapModels(IEnumerable<ScheduledTaskModel> items) => items;

    protected override DataListViewModel<ScheduledTaskModel> MakeListModel() => new();
}
