using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.AdminUI.Models;

/// <summary>One page row in the account -> content tree overview.</summary>
public sealed class AccessNodeModel : DataViewModelBase
{
    public int PageId { get; set; }

    public string AccountKey { get; set; } = string.Empty;

    /// <summary>null = website header row (no verdict).</summary>
    public bool? VisibleState { get; set; }

    /// <summary>PermissionLevel bit value backing the Level column badge.</summary>
    public int LevelValue { get; set; }

    /// <summary>AccessOrigin name backing the Why column badge ("" for website rows).</summary>
    public string OriginKind { get; set; } = string.Empty;

    [ConfigurableProperty("Page")]
    public string Name { get; set; } = string.Empty;

    [ConfigurableProperty("Sees it")]
    public string Visible { get; set; } = string.Empty;

    [ConfigurableProperty("Level")]
    public string Level { get; set; } = string.Empty;

    [ConfigurableProperty("Why")]
    public string Origin { get; set; } = string.Empty;

    [ConfigurableProperty("Warnings")]
    public string Warning { get; set; } = string.Empty;
}
