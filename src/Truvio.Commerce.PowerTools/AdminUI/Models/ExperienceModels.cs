using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.AdminUI.Models;

/// <summary>
/// The Experience Analyzer report: one account (against the anonymous baseline) or two
/// accounts side by side, as a headline plus rendered sections.
/// </summary>
public sealed class ExperienceAnalyzerModel : DataViewModelBase
{
    /// <summary>Short title — the breadcrumb node already says which tool this is.</summary>
    public string Title { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;

    /// <summary>Empty in single-account mode.</summary>
    public string CompareName { get; set; } = string.Empty;

    public bool Comparing { get; set; }

    public string Scope { get; set; } = string.Empty;

    /// <summary>"12 of 40" — pages the first account sees.</summary>
    public string VisibleA { get; set; } = string.Empty;

    public string VisibleB { get; set; } = string.Empty;

    public int DifferenceCount { get; set; }

    public bool Identical { get; set; }

    public string Error { get; set; } = string.Empty;

    public List<ReportSectionModel> Sections { get; set; } = [];
}

/// <summary>The "Why?" slide-over for one page: both sides' full explanations.</summary>
public sealed class ExperienceWhyModel : DataViewModelBase
{
    public string Heading { get; set; } = string.Empty;

    public string Html { get; set; } = string.Empty;
}
