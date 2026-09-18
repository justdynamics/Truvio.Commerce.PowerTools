using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Core.Rules;

/// <summary>
/// OPS-W1 — an enabled task whose most recent run failed. DW keeps the outcome on the task
/// itself (<c>ScheduledTask.TaskLastResult</c> / <c>TaskLastException</c>), so a failure is
/// visible without reading a single log file — but nothing in the admin surfaces it as an
/// alert, which is why a broken nightly import can go unnoticed for weeks.
/// </summary>
public sealed class FailingTaskRule : IOperationsRule
{
    public const string Id = "OPS-W1";

    public IEnumerable<Finding> Evaluate(OperationsSnapshot snapshot)
    {
        foreach (var task in snapshot.Tasks.Where(t => t.Enabled && t.LastRunFailed))
        {
            var detail = string.IsNullOrWhiteSpace(task.LastException)
                ? "DW recorded the last run as failed but stored no exception text."
                : Truncate(task.LastException, 400);

            yield return new Finding(
                Id,
                FindingSeverity.Critical,
                OperationsEntities.ScheduledTask,
                task.Id.ToString(),
                $"{task.Name} (#{task.Id})",
                $"Last run failed {OpsFormat.Relative(task.LastRun, snapshot.Now)}",
                detail);
        }
    }

    internal static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + " …";
}
