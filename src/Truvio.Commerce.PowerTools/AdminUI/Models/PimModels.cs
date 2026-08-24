using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.AdminUI.Models;

/// <summary>
/// One product family row in the Completeness explorer. Family rows, not variant rows: DW's
/// own list scores a family, and a 12k-part catalog with variants would otherwise render tens
/// of thousands of rows. Variants unfold on the drill-down screen.
/// </summary>
public sealed class PimCompletenessModel : DataViewModelBase
{
    /// <summary>Carried for the row action, not shown as a column.</summary>
    public string ProductId { get; set; } = string.Empty;

    public string LanguageId { get; set; } = string.Empty;

    /// <summary>Drives the score badge colour; not a column of its own.</summary>
    public int ScoreValue { get; set; }

    [ConfigurableProperty("Number", isSearchable: true)]
    public string Number { get; set; } = string.Empty;

    [ConfigurableProperty("Product", isSearchable: true)]
    public string Name { get; set; } = string.Empty;

    [ConfigurableProperty("Complete")]
    public string Score { get; set; } = string.Empty;

    [ConfigurableProperty("Worst rule", isSearchable: true)]
    public string WorstRule { get; set; } = string.Empty;

    [ConfigurableProperty("Missing")]
    public string MissingCount { get; set; } = string.Empty;

    [ConfigurableProperty("Missing fields", isSearchable: true)]
    public string MissingFields { get; set; } = string.Empty;
}

/// <summary>The per-product quality report (overview screen).</summary>
public sealed class PimProductQualityModel : DataViewModelBase
{
    public string Title { get; set; } = string.Empty;

    public string ProductId { get; set; } = string.Empty;

    public string LanguageId { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string Score { get; set; } = string.Empty;

    public int ScoreValue { get; set; }

    public string RulesApplied { get; set; } = string.Empty;

    public string MissingCount { get; set; } = string.Empty;

    public string LanguagesBehind { get; set; } = string.Empty;

    public string Error { get; set; } = string.Empty;

    /// <summary>Rendered sections, each a heading plus an HTML table.</summary>
    public List<PimSectionModel> Sections { get; set; } = [];
}

/// <summary>The catalog quality report (overview screen, the section landing).</summary>
public sealed class PimQualityModel : DataViewModelBase
{
    public string Verdict { get; set; } = string.Empty;

    public bool Healthy { get; set; }

    public string ProductsScanned { get; set; } = string.Empty;

    public string AverageScore { get; set; } = string.Empty;

    public string BelowThreshold { get; set; } = string.Empty;

    public string VariantGaps { get; set; } = string.Empty;

    public string BrokenImages { get; set; } = string.Empty;

    public string DeadRules { get; set; } = string.Empty;

    public string WorstField { get; set; } = string.Empty;

    public string FindingCounts { get; set; } = string.Empty;

    public string Error { get; set; } = string.Empty;

    /// <summary>The "fix this first" ranking.</summary>
    public List<OpsRowModel> WorstFields { get; set; } = [];

    public List<OpsRowModel> Findings { get; set; } = [];

    public string ScopeNote { get; set; } = string.Empty;
}

/// <summary>One completion rule / workflow row in the governance list.</summary>
public sealed class PimGovernanceModel : DataViewModelBase
{
    /// <summary>Drives the state badge; not a column of its own.</summary>
    public string State { get; set; } = string.Empty;

    [ConfigurableProperty("Kind", isSearchable: true)]
    public string Kind { get; set; } = string.Empty;

    [ConfigurableProperty("Name", isSearchable: true)]
    public string Name { get; set; } = string.Empty;

    [ConfigurableProperty("Status", isSearchable: true)]
    public string Status { get; set; } = string.Empty;

    [ConfigurableProperty("Applies to", isSearchable: true)]
    public string AppliesTo { get; set; } = string.Empty;

    [ConfigurableProperty("Fields")]
    public string Fields { get; set; } = string.Empty;
}

/// <summary>A heading plus rendered rows — one block on a PIM report screen.</summary>
public sealed class PimSectionModel
{
    public string Heading { get; set; } = string.Empty;

    public List<OpsRowModel> Rows { get; set; } = [];
}
