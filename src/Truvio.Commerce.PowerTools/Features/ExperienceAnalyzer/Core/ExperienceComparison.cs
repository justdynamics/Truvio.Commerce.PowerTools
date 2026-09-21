namespace Truvio.Commerce.PowerTools.Features.ExperienceAnalyzer.Core;

/// <summary>
/// What two accounts experience, side by side. The buckets are deliberately neutral — the
/// screen decides whether "B" reads as the compared account or as the anonymous baseline.
/// </summary>
public sealed record ExperienceComparison(
    string LabelA,
    string LabelB,
    bool BaselineMode,
    IReadOnlyList<WebsiteTally> Tallies,
    IReadOnlyList<ExperienceDifference> OnlyA,
    IReadOnlyList<ExperienceDifference> OnlyB,
    IReadOnlyList<ExperienceDifference> Both,
    IReadOnlyList<ExperienceDifference> Neither)
{
    public int TotalPages => Tallies.Sum(t => t.Total);

    public int VisibleToA => Tallies.Sum(t => t.VisibleA);

    public int VisibleToB => Tallies.Sum(t => t.VisibleB);

    /// <summary>Pages the two sides disagree about — the number the demo lives on.</summary>
    public int DifferenceCount => OnlyA.Count + OnlyB.Count;

    /// <summary>True when both sides see exactly the same pages (worth saying out loud).</summary>
    public bool Identical => DifferenceCount == 0;
}
