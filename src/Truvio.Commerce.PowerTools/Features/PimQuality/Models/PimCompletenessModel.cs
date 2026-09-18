using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Models;

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
