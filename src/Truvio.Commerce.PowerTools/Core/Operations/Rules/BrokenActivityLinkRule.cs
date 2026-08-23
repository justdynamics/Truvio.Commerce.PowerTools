using Truvio.Commerce.PowerTools.Core.Diagnostics;

namespace Truvio.Commerce.PowerTools.Core.Operations.Rules;

/// <summary>
/// OPS-W4/W5 — the scheduled-task ↔ activity link, checked in both directions.
/// <list type="bullet">
/// <item>OPS-W4: a task points at an activity identifier that no job file backs. DW resolves
/// the identifier lazily at run time, so the task looks perfectly healthy in the task list and
/// only fails when it fires — the classic "renamed the job, forgot the task" breakage.</item>
/// <item>OPS-W5: an activity that no enabled task runs. Not always wrong (plenty of activities
/// are run by hand), so it is informational — but a nightly import nobody schedules is worth
/// seeing.</item>
/// </list>
/// </summary>
public sealed class BrokenActivityLinkRule : IOperationsRule
{
    public const string BrokenId = "OPS-W4";
    public const string UnscheduledId = "OPS-W5";

    /// <summary>Above this many unscheduled activities, OPS-W5 reports one aggregated finding.</summary>
    public const int AggregateThreshold = 5;

    public IEnumerable<Finding> Evaluate(OperationsSnapshot snapshot)
    {
        foreach (var task in snapshot.Tasks.Where(t => ActivityLinks.IsBroken(t, snapshot.Activities)))
        {
            yield return new Finding(
                BrokenId,
                snapshot.Activities.Count == 0 ? FindingSeverity.Warning : FindingSeverity.Critical,
                OperationsEntities.ScheduledTask,
                task.Id.ToString(),
                $"{task.Name} (#{task.Id})",
                $"Runs a missing activity: {task.LinkedActivityId}",
                snapshot.Activities.Count == 0
                    ? "No data-integration activities were found at all, so the reference cannot be verified — " +
                      "check that the integration jobs folder is present."
                    : $"No job file matches the identifier '{task.LinkedActivityId}'. The activity was renamed, " +
                      "moved to another group, or deleted; the task will fail the next time it fires.");
        }

        var linked = ActivityLinks.TasksByActivity(snapshot.Tasks.Where(t => t.Enabled));
        var unscheduled = snapshot.Activities
            .Where(a => !linked[ActivityLinks.Normalise(a.Id)].Any())
            .ToList();

        // On an install where nothing is scheduled, one finding per activity buries everything
        // else under identical rows. Past the threshold they collapse into a single entry that
        // still names them.
        if (unscheduled.Count > AggregateThreshold)
        {
            yield return new Finding(
                UnscheduledId,
                FindingSeverity.Info,
                OperationsEntities.Activity,
                "*",
                $"{unscheduled.Count} activities",
                "No enabled scheduled task runs them",
                "They can only be started by hand: " + string.Join(", ", unscheduled.Select(a => a.Name)) + ".");
            yield break;
        }

        foreach (var activity in unscheduled)
        {
            yield return new Finding(
                UnscheduledId,
                FindingSeverity.Info,
                OperationsEntities.Activity,
                activity.Id,
                activity.Name,
                "No enabled scheduled task runs this activity",
                activity.LastRun is null
                    ? "The activity has never run and nothing schedules it."
                    : $"Last run {OpsFormat.Relative(activity.LastRun, snapshot.Now)}; it can only have been started by hand.");
        }
    }
}
