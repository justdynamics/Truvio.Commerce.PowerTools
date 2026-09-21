using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Models;

/// <summary>A heading plus a pre-rendered HTML table, used by the search overview screens.</summary>
public sealed class ReportSectionModel : DataViewModelBase
{
    public string Heading { get; set; } = string.Empty;

    public string Html { get; set; } = string.Empty;
}
