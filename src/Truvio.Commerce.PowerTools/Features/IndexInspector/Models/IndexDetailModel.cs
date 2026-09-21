using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Models;

/// <summary>The index detail report (overview screen).</summary>
public sealed class IndexDetailModel : DataViewModelBase
{
    public string Title { get; set; } = string.Empty;

    public string Repository { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    public string Builder { get; set; } = string.Empty;

    public string Balancer { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string HealthKind { get; set; } = string.Empty;

    public string StatusDetail { get; set; } = string.Empty;

    public string Documents { get; set; } = string.Empty;

    public string FieldCount { get; set; } = string.Empty;

    public bool IsProductIndex { get; set; }

    public string Error { get; set; } = string.Empty;

    /// <summary>Rendered sections, each a heading plus an HTML table.</summary>
    public List<ReportSectionModel> Sections { get; set; } = [];
}
