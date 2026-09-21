using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Models;

/// <summary>The activity detail report.</summary>
public sealed class IntegrationActivityDetailModel : DataViewModelBase
{
    public string ActivityId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public string LastRun { get; set; } = string.Empty;

    public string LastResult { get; set; } = string.Empty;

    public string Error { get; set; } = string.Empty;

    public List<OpsRowModel> Definition { get; set; } = [];

    public List<OpsRowModel> Tasks { get; set; } = [];

    public List<string> LogTail { get; set; } = [];
}
