using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Models;

/// <summary>One index row in the document browser's index picker.</summary>
public sealed class IndexPickModel : DataViewModelBase
{
    public string RepositoryName { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    [ConfigurableProperty("Repository", isSearchable: true)]
    public string Repository { get; set; } = string.Empty;

    [ConfigurableProperty("Index", isSearchable: true)]
    public string Index { get; set; } = string.Empty;

    [ConfigurableProperty("Online instance", isSearchable: true)]
    public string Instance { get; set; } = string.Empty;

    [ConfigurableProperty("Documents")]
    public string Documents { get; set; } = string.Empty;

    [ConfigurableProperty("Status", isSearchable: true)]
    public string Status { get; set; } = string.Empty;

    public string HealthKind { get; set; } = string.Empty;
}
