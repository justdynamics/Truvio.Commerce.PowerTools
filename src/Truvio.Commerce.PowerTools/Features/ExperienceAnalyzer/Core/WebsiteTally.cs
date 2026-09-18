namespace Truvio.Commerce.PowerTools.Features.ExperienceAnalyzer.Core;

/// <summary>Per-website totals, so "12 of 40 pages" is visible before any list is read.</summary>
public sealed record WebsiteTally(int AreaId, string AreaName, int Total, int VisibleA, int VisibleB);
