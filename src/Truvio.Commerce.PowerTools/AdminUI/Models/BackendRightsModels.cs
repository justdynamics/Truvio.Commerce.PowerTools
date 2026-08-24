using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.AdminUI.Models;

/// <summary>One row of the backend-user picker.</summary>
public sealed class BackendUserModel : DataViewModelBase
{
    /// <summary>Round-trip key, shared with the other Security screens: "user:17".</summary>
    public string AccountKey { get; set; } = string.Empty;

    [ConfigurableProperty("User", isSearchable: true)]
    public string Name { get; set; } = string.Empty;

    [ConfigurableProperty("Username", isSearchable: true)]
    public string UserName { get; set; } = string.Empty;

    [ConfigurableProperty("Backend access")]
    public string BackendAccess { get; set; } = string.Empty;

    [ConfigurableProperty("Status")]
    public string Status { get; set; } = string.Empty;
}

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

/// <summary>The "Why?" slide-over for one area, section or node.</summary>
public sealed class BackendRightsWhyModel : DataViewModelBase
{
    public string Heading { get; set; } = string.Empty;

    public string Html { get; set; } = string.Empty;
}
