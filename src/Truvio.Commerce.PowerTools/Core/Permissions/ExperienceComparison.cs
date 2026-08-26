namespace Truvio.Commerce.PowerTools.Core.Permissions;

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

/// <summary>Per-website totals, so "12 of 40 pages" is visible before any list is read.</summary>
public sealed record WebsiteTally(int AreaId, string AreaName, int Total, int VisibleA, int VisibleB);

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

/// <summary>
/// Compares what two accounts can see across the content tree. Pure: it takes the verdicts the
/// evaluator already produced and buckets them, so the standouts are unit-testable without a
/// Dynamicweb host.
/// <para>
/// The single-account mode is the same computation against the anonymous role as the B side:
/// pages only the account sees are what its groups earn it, and pages only anonymous sees are
/// a misconfiguration smell — a signed-in account losing content the public already has is
/// almost never intended.
/// </para>
/// </summary>
public static class ExperienceComparer
{
    /// <summary>Rows listed per section before the report truncates it.</summary>
    public const int DefaultSectionCap = 100;

    public static ExperienceComparison Compare(
        string labelA,
        IReadOnlyList<ExperiencePage> a,
        string labelB,
        IReadOnlyList<ExperiencePage> b,
        bool baselineMode = false)
    {
        var byIdB = b.GroupBy(p => p.PageId).ToDictionary(g => g.Key, g => g.First());
        var seen = new HashSet<int>();

        var onlyA = new List<ExperienceDifference>();
        var onlyB = new List<ExperienceDifference>();
        var both = new List<ExperienceDifference>();
        var neither = new List<ExperienceDifference>();
        var tallies = new Dictionary<int, (string Name, int Total, int VisibleA, int VisibleB)>();

        foreach (var pageA in a)
        {
            if (!seen.Add(pageA.PageId))
                continue; // a page appears once per account

            byIdB.TryGetValue(pageA.PageId, out var pageB);
            var visibleB = pageB?.Visible ?? false;
            var whyB = pageB?.Explanation ?? "Not evaluated for this account.";

            Bucket(new ExperienceDifference(
                pageA.PageId, pageA.AreaName, pageA.Path, pageA.Visible, visibleB, pageA.Explanation, whyB,
                pageA.ShortWhy, pageB?.ShortWhy ?? "Not evaluated"));

            Tally(pageA.AreaId, pageA.AreaName, pageA.Visible, visibleB);
        }

        // Pages the B side knows and the A side does not (scopes should match, but never lose a row).
        foreach (var pageB in b)
        {
            if (!seen.Add(pageB.PageId))
                continue;

            Bucket(new ExperienceDifference(
                pageB.PageId, pageB.AreaName, pageB.Path, false, pageB.Visible,
                "Not evaluated for this account.", pageB.Explanation,
                "Not evaluated", pageB.ShortWhy));

            Tally(pageB.AreaId, pageB.AreaName, visibleA: false, pageB.Visible);
        }

        return new ExperienceComparison(
            labelA,
            labelB,
            baselineMode,
            tallies
                .Select(t => new WebsiteTally(t.Key, t.Value.Name, t.Value.Total, t.Value.VisibleA, t.Value.VisibleB))
                .OrderBy(t => t.AreaName, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Sort(onlyA),
            Sort(onlyB),
            Sort(both),
            Sort(neither));

        void Bucket(ExperienceDifference difference)
        {
            var target = (difference.VisibleA, difference.VisibleB) switch
            {
                (true, false) => onlyA,
                (false, true) => onlyB,
                (true, true) => both,
                _ => neither
            };
            target.Add(difference);
        }

        void Tally(int areaId, string areaName, bool visibleA, bool visibleB)
        {
            var current = tallies.TryGetValue(areaId, out var existing)
                ? existing
                : (Name: areaName, Total: 0, VisibleA: 0, VisibleB: 0);

            tallies[areaId] = (
                current.Name,
                current.Total + 1,
                current.VisibleA + (visibleA ? 1 : 0),
                current.VisibleB + (visibleB ? 1 : 0));
        }

        static IReadOnlyList<ExperienceDifference> Sort(List<ExperienceDifference> items) =>
            items
                .OrderBy(d => d.AreaName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(d => d.Path, StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    /// <summary>
    /// The first <paramref name="cap"/> rows plus how many were left out — a content tree can
    /// carry thousands of pages, and a report nobody scrolls to the end of is worse than a
    /// truncated one that says so.
    /// </summary>
    public static (IReadOnlyList<T> Shown, int Hidden) Cap<T>(IReadOnlyList<T> items, int cap = DefaultSectionCap)
    {
        if (cap <= 0 || items.Count <= cap)
            return (items, 0);

        return (items.Take(cap).ToList(), items.Count - cap);
    }
}
