using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Models;

/// <summary>The health headline.</summary>
public sealed class OperationsHealthModel : DataViewModelBase
{
    public string Error { get; set; } = string.Empty;

    public string Verdict { get; set; } = string.Empty;

    public bool Healthy { get; set; }

    public string Tasks { get; set; } = string.Empty;

    public string FailingTasks { get; set; } = string.Empty;

    public string StaleTasks { get; set; } = string.Empty;

    public string BrokenLinks { get; set; } = string.Empty;

    public string Storage { get; set; } = string.Empty;

    public string LargestBloat { get; set; } = string.Empty;

    /// <summary>e.g. "0 critical, 2 warning, 1 info" — keeps "Healthy" honest next to a long list.</summary>
    public string FindingCounts { get; set; } = string.Empty;

    public List<OpsRowModel> Findings { get; set; } = [];
}
