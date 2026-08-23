using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.AdminUI.Models;

public sealed class FindingModel : DataViewModelBase
{
    [ConfigurableProperty("Severity")]
    public string Severity { get; set; } = string.Empty;

    [ConfigurableProperty("Rule", isSearchable: true)]
    public string RuleId { get; set; } = string.Empty;

    [ConfigurableProperty("Content", isSearchable: true)]
    public string Entity { get; set; } = string.Empty;

    [ConfigurableProperty("Finding", isSearchable: true)]
    public string Title { get; set; } = string.Empty;

    [ConfigurableProperty("Detail", isSearchable: true)]
    public string Detail { get; set; } = string.Empty;
}
