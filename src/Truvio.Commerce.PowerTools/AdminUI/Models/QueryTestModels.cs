using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.AdminUI.Models;

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

/// <summary>One declared parameter (or tester setting) in the "Set parameters" step.</summary>
public sealed class QueryParameterModel : DataViewModelBase
{
    public string RepositoryName { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    public string ParameterName { get; set; } = string.Empty;

    public string StateKind { get; set; } = string.Empty;

    [ConfigurableProperty("Parameter", isSearchable: true)]
    public string Name { get; set; } = string.Empty;

    [ConfigurableProperty("Type", isSearchable: true)]
    public string Type { get; set; } = string.Empty;

    [ConfigurableProperty("Default", isSearchable: true)]
    public string Default { get; set; } = string.Empty;

    [ConfigurableProperty("Value for this run", isSearchable: true)]
    public string Value { get; set; } = string.Empty;

    [ConfigurableProperty("Effect", isSearchable: true)]
    public string Effect { get; set; } = string.Empty;
}

/// <summary>The Query tester report (overview screen).</summary>
public sealed class QueryTestModel : DataViewModelBase
{
    public string Title { get; set; } = string.Empty;

    public string Repository { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    public string QueryName { get; set; } = string.Empty;

    public string IndexName { get; set; } = string.Empty;

    public string Instance { get; set; } = string.Empty;

    public string Hits { get; set; } = string.Empty;

    public string Took { get; set; } = string.Empty;

    /// <summary>"ok" / "warn" / "bad" — drives the info-bar badge.</summary>
    public string VerdictKind { get; set; } = string.Empty;

    public string Verdict { get; set; } = string.Empty;

    public string Error { get; set; } = string.Empty;

    /// <summary>Set when the source index has no product/content documents to show.</summary>
    public string Notice { get; set; } = string.Empty;

    public List<ReportSectionModel> Sections { get; set; } = [];
}
