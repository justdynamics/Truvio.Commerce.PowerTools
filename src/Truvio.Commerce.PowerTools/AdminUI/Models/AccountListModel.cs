using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.AdminUI.Models;

public sealed class AccountListModel : DataViewModelBase
{
    /// <summary>Round-trip key: "role:Anonymous" / "group:42" / "user:17".</summary>
    public string AccountKey { get; set; } = string.Empty;

    /// <summary>True for accounts that bypass permission checks entirely.</summary>
    public bool IsAdmin { get; set; }

    [ConfigurableProperty("Type")]
    public string Kind { get; set; } = string.Empty;

    [ConfigurableProperty("Account", isSearchable: true)]
    public string Name { get; set; } = string.Empty;

    [ConfigurableProperty("Details", isSearchable: true)]
    public string Detail { get; set; } = string.Empty;
}
