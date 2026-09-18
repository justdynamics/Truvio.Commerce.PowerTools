using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Models;

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
