using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Models;

/// <summary>The task detail report — rendered as sections, not as a grid.</summary>
public sealed class ScheduledTaskDetailModel : DataViewModelBase
{
    public int TaskId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string AddIn { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string LastRun { get; set; } = string.Empty;

    public string NextRun { get; set; } = string.Empty;

    public string Error { get; set; } = string.Empty;

    public List<OpsRowModel> Definition { get; set; } = [];

    public List<OpsRowModel> Parameters { get; set; } = [];

    public List<OpsRowModel> Runs { get; set; } = [];

    public string RunSourceNote { get; set; } = string.Empty;

    public string LastException { get; set; } = string.Empty;
}
