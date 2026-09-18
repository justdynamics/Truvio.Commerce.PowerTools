using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.QueryTester.Models;

/// <summary>One query row in the Query tester's query picker.</summary>
public sealed class QueryPickModel : DataViewModelBase
{
    public string RepositoryName { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    public string HealthKind { get; set; } = string.Empty;

    [ConfigurableProperty("Repository", isSearchable: true)]
    public string Repository { get; set; } = string.Empty;

    [ConfigurableProperty("Query", isSearchable: true)]
    public string Query { get; set; } = string.Empty;

    [ConfigurableProperty("Source index", isSearchable: true)]
    public string Source { get; set; } = string.Empty;

    /// <summary>"33 (0 with a default)" — the blank-parameter risk at a glance.</summary>
    [ConfigurableProperty("Parameters")]
    public string Parameters { get; set; } = string.Empty;

    [ConfigurableProperty("Index status", isSearchable: true)]
    public string Status { get; set; } = string.Empty;
}
