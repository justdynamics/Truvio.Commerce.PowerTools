using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.AdminUI.Models;

/// <summary>One index row in "Repositories &amp; indexes".</summary>
public sealed class IndexListModel : DataViewModelBase
{
    public string RepositoryName { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    public string HealthKind { get; set; } = string.Empty;

    [ConfigurableProperty("Repository")]
    public string Repository { get; set; } = string.Empty;

    [ConfigurableProperty("Index")]
    public string Index { get; set; } = string.Empty;

    [ConfigurableProperty("Builder")]
    public string Builder { get; set; } = string.Empty;

    [ConfigurableProperty("Fields")]
    public string Fields { get; set; } = string.Empty;

    [ConfigurableProperty("Last build")]
    public string LastBuild { get; set; } = string.Empty;

    [ConfigurableProperty("Status")]
    public string Status { get; set; } = string.Empty;
}

/// <summary>The index detail report (overview screen).</summary>
public sealed class IndexDetailModel : DataViewModelBase
{
    public string Title { get; set; } = string.Empty;

    public string Repository { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    public string Builder { get; set; } = string.Empty;

    public string Balancer { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string HealthKind { get; set; } = string.Empty;

    public string StatusDetail { get; set; } = string.Empty;

    public string Documents { get; set; } = string.Empty;

    public string FieldCount { get; set; } = string.Empty;

    public bool IsProductIndex { get; set; }

    public string Error { get; set; } = string.Empty;

    /// <summary>Rendered sections, each a heading plus an HTML table.</summary>
    public List<ReportSectionModel> Sections { get; set; } = [];
}

/// <summary>A heading plus a pre-rendered HTML table, used by the search overview screens.</summary>
public sealed class ReportSectionModel : DataViewModelBase
{
    public string Heading { get; set; } = string.Empty;

    public string Html { get; set; } = string.Empty;
}

/// <summary>One field row in "Field where-used".</summary>
public sealed class FieldUsageModel : DataViewModelBase
{
    public string StatusKind { get; set; } = string.Empty;

    [ConfigurableProperty("Field")]
    public string Field { get; set; } = string.Empty;

    [ConfigurableProperty("Index")]
    public string Index { get; set; } = string.Empty;

    /// <summary>Type and index flags in one column — the list grid gives every column the
    /// same width, so five columns is the readable maximum here.</summary>
    [ConfigurableProperty("Type")]
    public string Type { get; set; } = string.Empty;

    [ConfigurableProperty("Used by")]
    public string UsedBy { get; set; } = string.Empty;

    [ConfigurableProperty("Status")]
    public string Status { get; set; } = string.Empty;
}

/// <summary>One linter finding row.</summary>
public sealed class QueryLintModel : DataViewModelBase
{
    [ConfigurableProperty("Severity")]
    public string Severity { get; set; } = string.Empty;

    [ConfigurableProperty("Rule")]
    public string RuleId { get; set; } = string.Empty;

    [ConfigurableProperty("Where")]
    public string Entity { get; set; } = string.Empty;

    [ConfigurableProperty("Finding")]
    public string Title { get; set; } = string.Empty;

    [ConfigurableProperty("Detail")]
    public string Detail { get; set; } = string.Empty;
}

/// <summary>One index row in the document browser's index picker.</summary>
public sealed class IndexPickModel : DataViewModelBase
{
    public string RepositoryName { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    [ConfigurableProperty("Repository")]
    public string Repository { get; set; } = string.Empty;

    [ConfigurableProperty("Index")]
    public string Index { get; set; } = string.Empty;

    [ConfigurableProperty("Online instance")]
    public string Instance { get; set; } = string.Empty;

    [ConfigurableProperty("Documents")]
    public string Documents { get; set; } = string.Empty;

    [ConfigurableProperty("Status")]
    public string Status { get; set; } = string.Empty;

    public string HealthKind { get; set; } = string.Empty;
}

/// <summary>One document row in the document browser list.</summary>
public sealed class DocumentRowModel : DataViewModelBase
{
    public string RepositoryName { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    public int Ordinal { get; set; }

    public string MatchKind { get; set; } = string.Empty;

    [ConfigurableProperty("Key")]
    public string Key { get; set; } = string.Empty;

    [ConfigurableProperty("Label")]
    public string Label { get; set; } = string.Empty;

    [ConfigurableProperty("Summary")]
    public string Summary { get; set; } = string.Empty;

    [ConfigurableProperty("Database")]
    public string Match { get; set; } = string.Empty;
}

/// <summary>One document in full (overview screen).</summary>
public sealed class DocumentDetailModel : DataViewModelBase
{
    public string Title { get; set; } = string.Empty;

    public string Repository { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    public string IndexName { get; set; } = string.Empty;

    public string Instance { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string Match { get; set; } = string.Empty;

    public string MatchKind { get; set; } = string.Empty;

    public string FieldCount { get; set; } = string.Empty;

    public string Error { get; set; } = string.Empty;

    public List<ReportSectionModel> Sections { get; set; } = [];
}
