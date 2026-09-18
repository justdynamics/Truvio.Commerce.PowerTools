using Dynamicweb.CoreUI.Data;
using Dynamicweb.CoreUI.Data.DynamicFields;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Models;

namespace Truvio.Commerce.PowerTools.Features.QueryTester.Models;

/// <summary>The Query tester report (overview screen).</summary>
public sealed class QueryTestModel : DataViewModelBase
{
    public string Title { get; set; } = string.Empty;

    public string Repository { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    public string QueryName { get; set; } = string.Empty;

    public string IndexName { get; set; } = string.Empty;

    public string Instance { get; set; } = string.Empty;

    public string Hits { get; set; } = string.Empty;

    public string Took { get; set; } = string.Empty;

    /// <summary>"ok" / "warn" / "bad" — drives the info-bar badge.</summary>
    public string VerdictKind { get; set; } = string.Empty;

    public string Verdict { get; set; } = string.Empty;

    public string Error { get; set; } = string.Empty;

    /// <summary>Set when the source index has no product/content documents to show.</summary>
    public string Notice { get; set; } = string.Empty;

    public List<ReportSectionModel> Sections { get; set; } = [];
}
