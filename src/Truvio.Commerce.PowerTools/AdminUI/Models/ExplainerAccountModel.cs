using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.AdminUI.Models;

/// <summary>One pickable account for the Price Explainer: the anonymous visitor or a user.</summary>
public sealed class ExplainerAccountModel : DataViewModelBase
{
    /// <summary>"anonymous" or the user id; empty = informational row.</summary>
    public string AccountKey { get; set; } = string.Empty;

    [ConfigurableProperty("Type")]
    public string Kind { get; set; } = string.Empty;

    [ConfigurableProperty("Name")]
    public string Name { get; set; } = string.Empty;

    [ConfigurableProperty("Details")]
    public string Detail { get; set; } = string.Empty;
}
