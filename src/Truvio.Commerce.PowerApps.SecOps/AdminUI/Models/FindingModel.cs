using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerApps.SecOps.AdminUI.Models;

public sealed class FindingModel : DataViewModelBase
{
    [ConfigurableProperty("Severity")]
    public string Severity { get; set; } = string.Empty;

    [ConfigurableProperty("Rule")]
    public string RuleId { get; set; } = string.Empty;

    [ConfigurableProperty("Content")]
    public string Entity { get; set; } = string.Empty;

    [ConfigurableProperty("Finding")]
    public string Title { get; set; } = string.Empty;

    [ConfigurableProperty("Detail")]
    public string Detail { get; set; } = string.Empty;
}
