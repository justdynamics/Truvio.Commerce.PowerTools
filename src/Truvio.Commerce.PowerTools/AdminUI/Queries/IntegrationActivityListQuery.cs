using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Operations;
using Truvio.Commerce.PowerTools.Core.Operations.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// Every data-integration activity with its providers, its last run, and — the part DW's own
/// screens do not show — which scheduled task, if any, actually runs it.
/// </summary>
public sealed class IntegrationActivityListQuery : DataQueryListBase<IntegrationActivityModel, IntegrationActivityModel, DataListViewModel<IntegrationActivityModel>>
{
    protected override IEnumerable<IntegrationActivityModel>? GetListItems()
    {
        var source = new DwOperationsSource();
        var now = DateTime.Now;
        var activities = source.GetActivities();
        var tasks = source.GetTasks();
        var byActivity = ActivityLinks.TasksByActivity(tasks);

        var items = new List<IntegrationActivityModel>();
        var search = (Search ?? string.Empty).Trim();

        foreach (var activity in activities)
        {
            if (search.Length > 0
                && !activity.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                && !activity.Group.Contains(search, StringComparison.OrdinalIgnoreCase)
                && !activity.SourceShortName.Contains(search, StringComparison.OrdinalIgnoreCase)
                && !activity.DestinationShortName.Contains(search, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var linked = byActivity[ActivityLinks.Normalise(activity.Id)].ToList();

            items.Add(new IntegrationActivityModel
            {
                ActivityId = activity.Id,
                ResultKind = ResultKind(activity.LastResult),
                Name = string.IsNullOrEmpty(activity.Group) ? activity.Name : $"{activity.Group} / {activity.Name}",
                Source = activity.SourceShortName,
                Destination = activity.DestinationShortName,
                ScheduledBy = DescribeSchedule(linked),
                LastRun = activity.LastRun is null ? "never" : OpsFormat.Relative(activity.LastRun, now),
                LastResult = activity.LastResult
            });
        }

        // Tasks pointing at an activity that no longer exists get their own row: without it the
        // broken link is invisible on this screen, because there is nothing left to list.
        foreach (var task in tasks.Where(t => ActivityLinks.IsBroken(t, activities)))
        {
            if (search.Length > 0 && !task.LinkedActivityId.Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;

            items.Add(new IntegrationActivityModel
            {
                ActivityId = string.Empty,
                ResultKind = "missing",
                Name = task.LinkedActivityId,
                Source = "-",
                Destination = "-",
                ScheduledBy = $"{task.Name} (#{task.Id})",
                LastRun = "-",
                LastResult = "Missing"
            });
        }

        return items;
    }

    /// <summary>
    /// DW's <c>JobResult</c> values are Unknown / Completed / Failed / CompletedWithError; map
    /// them onto the badge colours the rest of the suite uses.
    /// </summary>
    internal static string ResultKind(string lastResult) => lastResult switch
    {
        "Completed" => "ok",
        "CompletedWithError" => "warn",
        "Failed" => "reject",
        "Missing" => "reject",
        _ => "info"
    };

    private static string DescribeSchedule(IReadOnlyList<TaskSpec> tasks) => tasks.Count switch
    {
        0 => "manual only",
        1 => tasks[0].Enabled ? tasks[0].Name : $"{tasks[0].Name} (disabled)",
        _ => $"{tasks.Count} tasks"
    };

    protected override IEnumerable<IntegrationActivityModel> MapModels(IEnumerable<IntegrationActivityModel> items) => items;

    protected override DataListViewModel<IntegrationActivityModel> MakeListModel() => new();
}
