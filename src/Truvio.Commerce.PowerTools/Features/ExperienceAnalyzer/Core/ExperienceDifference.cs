namespace Truvio.Commerce.PowerTools.Features.ExperienceAnalyzer.Core;

/// <summary>
/// One page where the two sides disagree — or agree. <paramref name="WhyA"/> and
/// <paramref name="WhyB"/> keep both explanations so the report can say what gates the side
/// that does not see it.
/// </summary>
public sealed record ExperienceDifference(
    int PageId,
    string AreaName,
    string Path,
    bool VisibleA,
    bool VisibleB,
    string WhyA,
    string WhyB,
    string ShortWhyA = "",
    string ShortWhyB = "");
