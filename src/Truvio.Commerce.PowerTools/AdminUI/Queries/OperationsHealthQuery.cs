using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Operations;
using Truvio.Commerce.PowerTools.Core.Operations.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// The one-screen answer to "is this install healthy": every rule run over one snapshot, with
/// the counts up front and the findings underneath.
/// </summary>
public sealed class OperationsHealthQuery : DataQueryModelBase<OperationsHealthModel>
{
    public override OperationsHealthModel? GetModel()
    {
        try
        {
            var health = new OperationsHealthEngine().Summarise(new DwOperationsSource().Snapshot());

            var model = new OperationsHealthModel
            {
                Verdict = health.Verdict,
                Healthy = health.CriticalCount == 0 && health.WarningCount == 0,
                Tasks = $"{health.EnabledTaskCount} of {health.TaskCount} enabled",
                FailingTasks = health.FailingTaskCount.ToString(),
                StaleTasks = health.StaleTaskCount.ToString(),
                BrokenLinks = health.BrokenLinkCount.ToString(),
                Storage = $"{OpsFormat.Bytes(health.LogBytes)} logs / {OpsFormat.Bytes(health.DatabaseBytes)} database",
                LargestBloat = health.LargestBloatFinding is { } bloat
                    ? $"{bloat.EntityDisplayName}: {bloat.Title}"
                    : "nothing outsized",
                FindingCounts = $"{health.CriticalCount} critical, {health.WarningCount} warning, {health.Findings.Count - health.CriticalCount - health.WarningCount} info"
            };

            model.Findings.AddRange(health.Findings.Select(LogsStorageQuery.ToRow));
            if (health.Findings.Count == 0)
            {
                model.Findings.Add(new OpsRowModel
                {
                    Item = "Findings",
                    Verdict = "none",
                    VerdictKind = "ok",
                    Why = "No task, activity, log folder or table looks wrong."
                });
            }

            return model;
        }
        catch (Exception ex)
        {
            return new OperationsHealthModel { Verdict = "Unavailable", Error = ex.Message };
        }
    }
}
