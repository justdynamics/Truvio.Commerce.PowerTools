using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Models;

/// <summary>One index row in "Repositories &amp; indexes".</summary>
public sealed class IndexListModel : DataViewModelBase
{
    public string RepositoryName { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    public string HealthKind { get; set; } = string.Empty;

    [ConfigurableProperty("Repository", isSearchable: true)]
    public string Repository { get; set; } = string.Empty;

    [ConfigurableProperty("Index", isSearchable: true)]
    public string Index { get; set; } = string.Empty;

    [ConfigurableProperty("Builder", isSearchable: true)]
    public string Builder { get; set; } = string.Empty;

    [ConfigurableProperty("Fields")]
    public string Fields { get; set; } = string.Empty;

    [ConfigurableProperty("Last build")]
    public string LastBuild { get; set; } = string.Empty;

    [ConfigurableProperty("Status", isSearchable: true)]
    public string Status { get; set; } = string.Empty;
}
