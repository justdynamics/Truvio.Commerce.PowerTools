using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Models;

namespace Truvio.Commerce.PowerTools.Features.BackendRights.Models;

/// <summary>The rights report as one overview model: headline facts plus rendered sections.</summary>
public sealed class BackendRightsModel : DataViewModelBase
{
    public string Title { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public bool BackendAccess { get; set; }

    public string Status { get; set; } = string.Empty;

    public string AreasVisible { get; set; } = string.Empty;

    public string GateInForce { get; set; } = string.Empty;

    public string Error { get; set; } = string.Empty;

    /// <summary>Rendered HTML per report section, in display order.</summary>
    public List<ReportSectionModel> Sections { get; set; } = [];
}
