using System.Globalization;
using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Operations;
using Truvio.Commerce.PowerTools.Core.Operations.Dw;
using Truvio.Commerce.PowerTools.Core.Settings.Dw;
using Truvio.Commerce.PowerTools.Core.Settings;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// One task in full: its definition, the parameters the add-in was saved with, and the last N
/// runs. Read-only — the parameters are shown, never edited.
/// </summary>
public sealed class ScheduledTaskDetailQuery : DataQueryModelBase<ScheduledTaskDetailModel>
{
    /// <summary>How many past runs the detail screen lists.</summary>
    public const int RunCount = 20;

    public int TaskId { get; set; }

    public override ScheduledTaskDetailModel? GetModel()
    {
        if (TaskId <= 0)
            return new ScheduledTaskDetailModel { Title = "Scheduled task", Error = "No task selected." };

        try
        {
            var source = new DwOperationsSource();
            var now = DateTime.Now;

            var task = source.GetTasks().FirstOrDefault(t => t.Id == TaskId);
            if (task is null)
                return new ScheduledTaskDetailModel { Title = "Scheduled task", Error = $"Task {TaskId} no longer exists." };

            var activities = source.GetActivities();
            var runs = source.GetTaskRuns(task.Id, PowerToolsSettings.Positive(DwPowerToolsSettings.Current.RunHistoryDepth, RunCount));
            var parameters = source.GetTaskParameters(task.Id);

            var model = new ScheduledTaskDetailModel
            {
                TaskId = task.Id,
                Title = task.Name,
                AddIn = task.AddInShortName,
                State = task.Enabled ? (task.LastRunFailed ? "failed" : "ok") : "disabled",
                Status = task.Enabled ? (task.LastRunFailed ? "Last run failed" : "Enabled") : "Disabled",
                LastRun = task.LastRun is null ? "never" : $"{OpsFormat.Absolute(task.LastRun)} ({OpsFormat.Relative(task.LastRun, now)})",
                NextRun = task.NextRun is null ? "-" : $"{OpsFormat.Absolute(task.NextRun)} ({OpsFormat.Relative(task.NextRun, now)})",
                LastException = task.LastException,
                RunSourceNote = runs.Count == 0
                    ? "DW has recorded no runs for this task."
                    : runs.All(r => r.TriggeredBy == "unknown")
                        ? "Reconstructed from the scheduler log files, which carry no attribution."
                        : "From DW's execution history; \"Scheduler\" means the run was unattended."
            };

            model.Definition.AddRange(BuildDefinition(task, activities, now));
            model.Parameters.AddRange(parameters.Select(p => new OpsRowModel
            {
                Item = p.Name,
                Value = Shorten(p.Value)
            }));
            if (parameters.Count == 0)
            {
                model.Parameters.Add(new OpsRowModel
                {
                    Item = "Parameters",
                    Verdict = "none",
                    Why = "The add-in was saved without parameters."
                });
            }

            model.Runs.AddRange(runs.Select(run => new OpsRowModel
            {
                Item = OpsFormat.Absolute(run.StartTime ?? run.ScheduleTime),
                Verdict = run.Succeeded switch { true => "OK", false => "Failed", _ => "Unknown" },
                VerdictKind = run.Succeeded switch { true => "ok", false => "reject", _ => "" },
                Value = OpsFormat.Duration(run.Duration),
                Why = BuildRunDetail(run)
            }));

            return model;
        }
        catch (Exception ex)
        {
            return new ScheduledTaskDetailModel { Title = "Scheduled task", Error = ex.Message };
        }
    }

    private static IEnumerable<OpsRowModel> BuildDefinition(TaskSpec task, IReadOnlyList<ActivitySpec> activities, DateTime now)
    {
        yield return new OpsRowModel { Item = "Id", Value = task.Id.ToString(CultureInfo.InvariantCulture) };
        yield return new OpsRowModel
        {
            Item = "Enabled",
            Verdict = task.Enabled ? "Yes" : "No",
            VerdictKind = task.Enabled ? "ok" : "warn"
        };
        yield return new OpsRowModel { Item = "Add-in", Value = task.AddInShortName, Why = task.AddInTypeName };
        yield return new OpsRowModel { Item = "Schedule", Value = OpsFormat.Interval(task.IntervalMinutes), Why = task.ScheduleSummary };
        yield return new OpsRowModel
        {
            Item = "Last run",
            Verdict = task.LastRun is null ? "never" : task.LastResult switch { true => "OK", false => "Failed", _ => "Unknown" },
            VerdictKind = task.LastRun is null ? "" : task.LastResult switch { true => "ok", false => "reject", _ => "" },
            Value = OpsFormat.Absolute(task.LastRun),
            Why = task.LastRun is null ? "DW has no record of this task ever running." : OpsFormat.Relative(task.LastRun, now)
        };
        yield return new OpsRowModel
        {
            Item = "Next run",
            Value = OpsFormat.Absolute(task.NextRun),
            Why = task.NextRun is null ? "No run is planned (the task is disabled or runs once)." : OpsFormat.Relative(task.NextRun, now)
        };

        if (!string.IsNullOrWhiteSpace(task.LinkedActivityId))
        {
            var exists = activities.Any(a => string.Equals(ActivityLinks.Normalise(a.Id), ActivityLinks.Normalise(task.LinkedActivityId), StringComparison.OrdinalIgnoreCase));
            yield return new OpsRowModel
            {
                Item = "Runs activity",
                Verdict = exists ? "Found" : "Missing",
                VerdictKind = exists ? "ok" : "reject",
                Value = task.LinkedActivityId,
                Why = exists
                    ? "The task runs this data-integration activity."
                    : "No job file matches this identifier — the task will fail the next time it fires."
            };
        }

        if (!string.IsNullOrWhiteSpace(task.Comment))
            yield return new OpsRowModel { Item = "Comment", Why = task.Comment };
    }

    private static string BuildRunDetail(TaskRunSpec run)
    {
        var parts = new List<string> { $"by {run.TriggeredBy}" };

        if (run.ScheduleTime is { } scheduled && run.StartTime is { } started && started > scheduled.AddSeconds(30))
            parts.Add($"started {OpsFormat.Duration(started - scheduled)} after its slot");

        if (run.EndTime is null && run.StartTime is not null)
            parts.Add("never recorded an end");

        if (!string.IsNullOrWhiteSpace(run.Message))
            parts.Add(Shorten(run.Message));

        return string.Join("; ", parts);
    }

    private static string Shorten(string value)
    {
        var single = value.Replace("\r", " ").Replace("\n", " ").Trim();
        return single.Length <= 300 ? single : single[..300] + " …";
    }
}
