namespace Truvio.Commerce.PowerTools.Features.ExperienceAnalyzer.Core;

/// <summary>One page as one account experiences it: where it sits, whether it is visible, and why.</summary>
/// <param name="ShortWhy">The compact gate label ("Gated here") for dense tables; empty falls back to the full text.</param>
public sealed record ExperiencePage(
    int PageId,
    int AreaId,
    string AreaName,
    string Path,
    bool Visible,
    string Explanation,
    string ShortWhy = "");
