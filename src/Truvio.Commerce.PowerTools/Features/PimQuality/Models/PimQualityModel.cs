using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Models;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Models;

/// <summary>The catalog quality report (overview screen, the section landing).</summary>
public sealed class PimQualityModel : DataViewModelBase
{
    public string Verdict { get; set; } = string.Empty;

    public bool Healthy { get; set; }

    public string ProductsScanned { get; set; } = string.Empty;

    public string AverageScore { get; set; } = string.Empty;

    public string BelowThreshold { get; set; } = string.Empty;

    public string VariantGaps { get; set; } = string.Empty;

    public string BrokenImages { get; set; } = string.Empty;

    public string DeadRules { get; set; } = string.Empty;

    public string WorstField { get; set; } = string.Empty;

    public string FindingCounts { get; set; } = string.Empty;

    public string Error { get; set; } = string.Empty;

    /// <summary>The "fix this first" ranking.</summary>
    public List<OpsRowModel> WorstFields { get; set; } = [];

    public List<OpsRowModel> Findings { get; set; } = [];

    public string ScopeNote { get; set; } = string.Empty;
}
