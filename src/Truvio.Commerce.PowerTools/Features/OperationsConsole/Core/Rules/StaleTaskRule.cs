using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Core.Rules;

/// <summary>
/// OPS-W2/W3 — enabled tasks that are not actually running.
/// <list type="bullet">
/// <item>OPS-W2: a repeating task whose last run is older than twice its own interval. Twice
/// the interval is the smallest window that does not fire on a run that merely started late.</item>
/// <item>OPS-W3: an enabled task that has never run at all.</item>
/// </list>
/// DW's own <c>Task.LastRunState</c> has a comparable "HasNotRunAsItShould" state, but it only
/// looks at whether <c>UpcomingRuntime</c> is in the past — it goes true the moment a run is a
/// second late, so it is unusable as an alert. This rule uses the interval instead.
/// </summary>
public sealed class StaleTaskRule : IOperationsRule
{
    public const string StaleId = "OPS-W2";
    public const string NeverRunId = "OPS-W3";

    /// <summary>How many intervals may pass before the task counts as stale.</summary>
    public const int IntervalTolerance = 2;

    private readonly int _tolerance;

    public StaleTaskRule() : this(IntervalTolerance)
    {
    }

    /// <summary>Configurable through PowerTools settings; a non-positive value falls back to the default.</summary>
    public StaleTaskRule(int intervalTolerance) =>
        _tolerance = intervalTolerance > 0 ? intervalTolerance : IntervalTolerance;

    public IEnumerable<Finding> Evaluate(OperationsSnapshot snapshot)
    {
        foreach (var task in snapshot.Tasks.Where(t => t.Enabled))
        {
            if (task.LastRun is null)
            {
                yield return new Finding(
                    NeverRunId,
                    FindingSeverity.Warning,
                    OperationsEntities.ScheduledTask,
                    task.Id.ToString(),
                    $"{task.Name} (#{task.Id})",
                    "Enabled but has never run",
                    $"The task is enabled ({OpsFormat.Interval(task.IntervalMinutes)}) but DW has no record of a single execution. " +
                    "Either the scheduler never reached it, or it was enabled and never triggered.");
                continue;
            }

            if (task.IntervalMinutes <= 0)
                continue;

            var overdueBy = snapshot.Now - task.LastRun.Value;
            var allowed = TimeSpan.FromMinutes((double)task.IntervalMinutes * _tolerance);
            if (overdueBy <= allowed)
                continue;

            yield return new Finding(
                StaleId,
                FindingSeverity.Warning,
                OperationsEntities.ScheduledTask,
                task.Id.ToString(),
                $"{task.Name} (#{task.Id})",
                $"Enabled but stale — last run {OpsFormat.Relative(task.LastRun, snapshot.Now)}",
                $"Runs {OpsFormat.Interval(task.IntervalMinutes)}, so at most " +
                $"{OpsFormat.Duration(allowed)} should pass between runs; {OpsFormat.Duration(overdueBy)} have. " +
                "A stopped scheduler, an overlapping long run, or an unhandled crash all look like this.");
        }
    }
}
