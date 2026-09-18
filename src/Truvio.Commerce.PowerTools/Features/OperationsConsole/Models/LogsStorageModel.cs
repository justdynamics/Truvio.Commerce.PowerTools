using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Models;

/// <summary>The logs &amp; storage report.</summary>
public sealed class LogsStorageModel : DataViewModelBase
{
    public string Error { get; set; } = string.Empty;

    public string LogTotal { get; set; } = string.Empty;

    public string DatabaseTotal { get; set; } = string.Empty;

    public string RetentionSummary { get; set; } = string.Empty;

    public bool RetentionEnabled { get; set; }

    public int FindingCount { get; set; }

    public List<OpsRowModel> Folders { get; set; } = [];

    public List<OpsRowModel> Tables { get; set; } = [];

    public List<OpsRowModel> Retention { get; set; } = [];

    public List<OpsRowModel> Findings { get; set; } = [];
}
