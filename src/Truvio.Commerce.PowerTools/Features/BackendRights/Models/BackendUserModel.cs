using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.BackendRights.Models;

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
