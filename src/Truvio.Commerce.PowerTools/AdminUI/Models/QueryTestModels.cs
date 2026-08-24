using Dynamicweb.CoreUI.Data;
using Dynamicweb.CoreUI.Data.DynamicFields;

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

/// <summary>
/// The "Set parameters" dialog: one dynamic field per declared parameter.
/// <see cref="IModelWithDynamicFields"/> is what makes the round-trip work: when the OK
/// command is posted, DW's model builder merges standard properties (Repository, Item) first,
/// then calls <see cref="FillDynamicFields"/> to rebuild the field set server-side, and only
/// then copies the posted values into those fields — without it the posted values are
/// silently dropped.
/// </summary>
public sealed class QueryValuesModel : DataViewModelBase, IModelWithDynamicFields
{
    public string QueryName { get; set; } = string.Empty;

    public string Repository { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    public FieldGroupCollection Fields { get; set; } = new();

    public void FillDynamicFields()
    {
        if (Fields.Groups.Any())
            return;

        Fields = Queries.QueryValuesQuery.BuildFields(Repository, Item, string.Empty);
    }
}

/// <summary>The "Why 'X'?" panel for one document (slide-over).</summary>
public sealed class QueryWhyModel : DataViewModelBase
{
    public string Heading { get; set; } = string.Empty;

    public string Html { get; set; } = string.Empty;
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
