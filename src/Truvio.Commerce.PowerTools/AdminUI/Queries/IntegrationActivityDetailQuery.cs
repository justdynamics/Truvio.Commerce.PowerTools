using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Operations;
using Truvio.Commerce.PowerTools.Core.Operations.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// One activity in full: the job definition summary, the scheduled tasks that run it, and the
/// tail of its newest run log.
/// </summary>
public sealed class IntegrationActivityDetailQuery : DataQueryModelBase<IntegrationActivityDetailModel>
{
    /// <summary>How many log lines the detail screen shows.</summary>
    public const int LogLines = 200;

    public string ActivityId { get; set; } = string.Empty;

    public override IntegrationActivityDetailModel? GetModel()
    {
        if (string.IsNullOrWhiteSpace(ActivityId))
            return new IntegrationActivityDetailModel { Title = "Integration activity", Error = "No activity selected." };

        try
        {
            var source = new DwOperationsSource();
            var now = DateTime.Now;

            var wanted = ActivityLinks.Normalise(ActivityId);
            var activity = source.GetActivities()
                .FirstOrDefault(a => string.Equals(ActivityLinks.Normalise(a.Id), wanted, StringComparison.OrdinalIgnoreCase));

            if (activity is null)
            {
                return new IntegrationActivityDetailModel
                {
                    ActivityId = ActivityId,
                    Title = ActivityId,
                    Error = $"No job file backs the identifier '{ActivityId}'. It was renamed, moved to another group, or deleted."
                };
            }

            var tasks = ActivityLinks.TasksFor(source.GetTasks(), activity);

            var model = new IntegrationActivityDetailModel
            {
                ActivityId = activity.Id,
                Title = string.IsNullOrEmpty(activity.Group) ? activity.Name : $"{activity.Group} / {activity.Name}",
                Source = activity.SourceShortName,
                Destination = activity.DestinationShortName,
                LastRun = activity.LastRun is null ? "never" : $"{OpsFormat.Absolute(activity.LastRun)} ({OpsFormat.Relative(activity.LastRun, now)})",
                LastResult = activity.LastResult
            };

            model.Definition.AddRange(BuildDefinition(activity));

            if (tasks.Count == 0)
            {
                model.Tasks.Add(new OpsRowModel
                {
                    Item = "Scheduled by",
                    Verdict = "none",
                    VerdictKind = "warn",
                    Why = "No scheduled task runs this activity; it can only be started by hand."
                });
            }
            else
            {
                model.Tasks.AddRange(tasks.Select(t => new OpsRowModel
                {
                    Item = $"{t.Name} (#{t.Id})",
                    Verdict = t.Enabled ? "Enabled" : "Disabled",
                    VerdictKind = t.Enabled ? "ok" : "warn",
                    Value = OpsFormat.Interval(t.IntervalMinutes),
                    Why = t.LastRun is null ? "Never run" : $"Last run {OpsFormat.Relative(t.LastRun, now)}"
                }));
            }

            model.LogTail.AddRange(source.GetActivityLogTail(activity.Id, LogLines));
            return model;
        }
        catch (Exception ex)
        {
            return new IntegrationActivityDetailModel { Title = ActivityId, Error = ex.Message };
        }
    }

    private static IEnumerable<OpsRowModel> BuildDefinition(ActivitySpec activity)
    {
        yield return new OpsRowModel { Item = "Identifier", Value = activity.Id, Why = "How a scheduled task references this activity." };

        if (!string.IsNullOrWhiteSpace(activity.Description))
            yield return new OpsRowModel { Item = "Description", Why = activity.Description };

        yield return new OpsRowModel { Item = "Source", Value = activity.SourceShortName, Why = activity.SourceProvider };
        yield return new OpsRowModel { Item = "Destination", Value = activity.DestinationShortName, Why = activity.DestinationProvider };
        yield return new OpsRowModel
        {
            Item = "Shape",
            Value = $"{activity.TableCount} table(s), {activity.MappingCount} mapping(s)",
            Why = $"{activity.ColumnMappingCount} column mapping(s) in total."
        };
        yield return new OpsRowModel
        {
            Item = "Last run",
            Verdict = activity.LastResult,
            VerdictKind = IntegrationActivityListQuery.ResultKind(activity.LastResult),
            Value = OpsFormat.Absolute(activity.LastRun),
            Why = activity.LastDuration is null
                ? "DW records the outcome in a marker file next to the run log."
                : $"Took {OpsFormat.Duration(activity.LastDuration)}."
        };
        yield return new OpsRowModel
        {
            Item = "Definition saved",
            Value = OpsFormat.Absolute(activity.ModifiedAt),
            Why = "Last write time of the job XML file."
        };
    }
}
