using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Models;

/// <summary>One completion rule / workflow row in the governance list.</summary>
public sealed class PimGovernanceModel : DataViewModelBase
{
    /// <summary>Drives the state badge; not a column of its own.</summary>
    public string State { get; set; } = string.Empty;

    [ConfigurableProperty("Kind", isSearchable: true)]
    public string Kind { get; set; } = string.Empty;

    [ConfigurableProperty("Name", isSearchable: true)]
    public string Name { get; set; } = string.Empty;

    [ConfigurableProperty("Status", isSearchable: true)]
    public string Status { get; set; } = string.Empty;

    [ConfigurableProperty("Applies to", isSearchable: true)]
    public string AppliesTo { get; set; } = string.Empty;

    [ConfigurableProperty("Fields")]
    public string Fields { get; set; } = string.Empty;
}
