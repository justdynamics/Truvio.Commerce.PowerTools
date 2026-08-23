using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.AdminUI.Models;

/// <summary>One line of the explanation report (sectioned: Context, Visibility, Price, Discounts, Result).</summary>
public sealed class ExplainRowModel : DataViewModelBase
{
    /// <summary>Badge styling hint for the Verdict column: "win", "match", "reject", "info", "warn", "ok", "hidden", "" (plain).</summary>
    public string VerdictKind { get; set; } = string.Empty;

    /// <summary>True for section header rows.</summary>
    public bool IsHeader { get; set; }

    [ConfigurableProperty("Section")]
    public string Section { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    [ConfigurableProperty("Verdict")]
    public string Verdict { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string Why { get; set; } = string.Empty;

    /// <summary>Item, Value and Why as one line — the column the screen shows.</summary>
    [ConfigurableProperty("Explanation")]
    public string Details { get; set; } = string.Empty;
}
