using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Models;

/// <summary>One document in full (overview screen).</summary>
public sealed class DocumentDetailModel : DataViewModelBase
{
    public string Title { get; set; } = string.Empty;

    public string Repository { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    public string IndexName { get; set; } = string.Empty;

    public string Instance { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string Match { get; set; } = string.Empty;

    public string MatchKind { get; set; } = string.Empty;

    public string FieldCount { get; set; } = string.Empty;

    public string Error { get; set; } = string.Empty;

    public List<ReportSectionModel> Sections { get; set; } = [];
}
