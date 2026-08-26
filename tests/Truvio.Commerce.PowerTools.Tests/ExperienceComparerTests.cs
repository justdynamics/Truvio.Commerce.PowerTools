using Truvio.Commerce.PowerTools.Core.Permissions;
using Truvio.Commerce.PowerTools.Core.Principals;
using Xunit;

namespace Truvio.Commerce.PowerTools.Tests;

/// <summary>
/// The comparer decides what "stands out" between two accounts. The demo it serves — one role
/// sees its dashboard, another sees theirs — lives or dies on these buckets being right.
/// </summary>
public class ExperienceComparerTests
{
    private const string Lumber = "Lumber Co.";
    private const string Roofing = "Roofing & Restoration";

    private static ExperiencePage Page(int id, bool visible, string path = "Home / Page", string area = "Site", string why = "Role default (Anonymous role)") =>
        new(id, 1, area, path, visible, why);

    // ---- Buckets --------------------------------------------------------------------------

    [Fact]
    public void PageOnlyOneSideSees_LandsInThatSidesBucket()
    {
        var a = new[] { Page(1, true, "Home / Lumber dashboard"), Page(2, false, "Home / Roofing dashboard") };
        var b = new[] { Page(1, false, "Home / Lumber dashboard"), Page(2, true, "Home / Roofing dashboard") };

        var result = ExperienceComparer.Compare(Lumber, a, Roofing, b);

        Assert.Equal("Home / Lumber dashboard", Assert.Single(result.OnlyA).Path);
        Assert.Equal("Home / Roofing dashboard", Assert.Single(result.OnlyB).Path);
        Assert.Empty(result.Both);
        Assert.Empty(result.Neither);
        Assert.Equal(2, result.DifferenceCount);
        Assert.False(result.Identical);
    }

    [Fact]
    public void SharedAndHiddenPages_LandInBothAndNeither()
    {
        var a = new[] { Page(1, true, "Home"), Page(2, false, "Home / Secret") };
        var b = new[] { Page(1, true, "Home"), Page(2, false, "Home / Secret") };

        var result = ExperienceComparer.Compare(Lumber, a, Roofing, b);

        Assert.Equal("Home", Assert.Single(result.Both).Path);
        Assert.Equal("Home / Secret", Assert.Single(result.Neither).Path);
        Assert.True(result.Identical);
        Assert.Equal(0, result.DifferenceCount);
    }

    [Fact]
    public void IdenticalExperiences_ReportNoDifferences()
    {
        var pages = new[] { Page(1, true), Page(2, true), Page(3, false) };

        var result = ExperienceComparer.Compare(Lumber, pages, Roofing, pages);

        Assert.True(result.Identical);
        Assert.Empty(result.OnlyA);
        Assert.Empty(result.OnlyB);
    }

    // ---- Baseline mode --------------------------------------------------------------------

    [Fact]
    public void BaselineMode_ExclusivePagesAreOnlyA()
    {
        // Gated to the account, invisible to the public: what its groups earn it.
        var account = new[] { Page(1, true, "Home / Dealer portal") };
        var anonymous = new[] { Page(1, false, "Home / Dealer portal") };

        var result = ExperienceComparer.Compare(Lumber, account, "Anonymous", anonymous, baselineMode: true);

        Assert.True(result.BaselineMode);
        Assert.Equal("Home / Dealer portal", Assert.Single(result.OnlyA).Path);
        Assert.Empty(result.OnlyB);
    }

    [Fact]
    public void BaselineMode_PublicPageHiddenFromAccount_IsFlaggedAsOnlyB()
    {
        // The misconfiguration smell: signing in LOSES content the public already has.
        var account = new[] { Page(1, false, "Home / News") };
        var anonymous = new[] { Page(1, true, "Home / News") };

        var result = ExperienceComparer.Compare(Lumber, account, "Anonymous", anonymous, baselineMode: true);

        Assert.Equal("Home / News", Assert.Single(result.OnlyB).Path);
        Assert.Empty(result.OnlyA);
    }

    // ---- Explanations ---------------------------------------------------------------------

    [Fact]
    public void BothExplanationsSurvive_SoTheReportCanSayWhoIsGated()
    {
        var a = new[] { Page(1, true, why: "Set here: 'Lumber' grants Read.") };
        var b = new[] { Page(1, false, why: "Gated here: 'Lumber' grants Read and 'Roofing' has no grant of its own.") };

        var difference = Assert.Single(ExperienceComparer.Compare(Lumber, a, Roofing, b).OnlyA);

        Assert.Contains("grants Read", difference.WhyA);
        Assert.Contains("no grant of its own", difference.WhyB);
    }

    [Fact]
    public void ExplanationFlowsFromTheRealEvaluator()
    {
        // End to end over the actual evaluator: a page granted to one group only.
        var source = new FakeContentSecuritySource();
        source.Areas.Add(new AreaNode(1, "Site"));
        source.Pages.Add(new PageNode(1, 0, 1, "Lumber dashboard", 1, true, false));
        source.Rows.Add(new ContentPermissionRow(SecurityAccount.AuthenticatedFrontendRole, ContentEntityNames.Page, "1", Levels.None));
        source.Rows.Add(new ContentPermissionRow("60", ContentEntityNames.Page, "1", Levels.Read));

        var evaluator = new EffectiveAccessEvaluator(source);
        var pagesById = source.GetPages(1).ToDictionary(p => p.Id);

        var lumber = new SecurityAccount
        {
            Kind = SecurityAccountKind.Group, Id = "60", DisplayName = Lumber,
            OwnerIds = [SecurityAccount.AuthenticatedFrontendRole, "60"]
        };
        var roofing = new SecurityAccount
        {
            Kind = SecurityAccountKind.Group, Id = "61", DisplayName = Roofing,
            OwnerIds = [SecurityAccount.AuthenticatedFrontendRole, "61"]
        };

        var pages = new List<(SecurityAccount Account, List<ExperiencePage> Pages)>
        {
            (lumber, []), (roofing, [])
        };

        foreach (var (account, list) in pages)
        {
            var access = evaluator.EvaluatePage(account, 1, pagesById);
            list.Add(new ExperiencePage(1, 1, "Site", "Lumber dashboard", access.GrantsRead,
                AccessExplanation.Explain(account, access, evaluator.GetExplicitPageRows(1), null, id => id ?? "none")));
        }

        var result = ExperienceComparer.Compare(Lumber, pages[0].Pages, Roofing, pages[1].Pages);

        Assert.Equal("Lumber dashboard", Assert.Single(result.OnlyA).Path);
        Assert.Contains("Only", Assert.Single(result.OnlyA).WhyB); // names who does get in
    }

    // ---- Tallies --------------------------------------------------------------------------

    [Fact]
    public void TalliesCountPerWebsite()
    {
        var a = new[]
        {
            new ExperiencePage(1, 1, "Lumber site", "Home", true, ""),
            new ExperiencePage(2, 1, "Lumber site", "Home / Sub", false, ""),
            new ExperiencePage(3, 2, "Roofing site", "Home", true, "")
        };
        var b = new[]
        {
            new ExperiencePage(1, 1, "Lumber site", "Home", true, ""),
            new ExperiencePage(2, 1, "Lumber site", "Home / Sub", true, ""),
            new ExperiencePage(3, 2, "Roofing site", "Home", false, "")
        };

        var result = ExperienceComparer.Compare(Lumber, a, Roofing, b);

        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.VisibleToA);
        Assert.Equal(2, result.VisibleToB);

        var lumberSite = result.Tallies.Single(t => t.AreaName == "Lumber site");
        Assert.Equal(2, lumberSite.Total);
        Assert.Equal(1, lumberSite.VisibleA);
        Assert.Equal(2, lumberSite.VisibleB);
    }

    [Fact]
    public void TalliesAreSortedByWebsiteName()
    {
        var pages = new[]
        {
            new ExperiencePage(1, 2, "Zebra site", "Home", true, ""),
            new ExperiencePage(2, 1, "Alpha site", "Home", true, "")
        };

        var result = ExperienceComparer.Compare(Lumber, pages, Roofing, pages);

        Assert.Equal(["Alpha site", "Zebra site"], result.Tallies.Select(t => t.AreaName));
    }

    [Fact]
    public void PageKnownToOneSideOnly_IsStillCounted()
    {
        var a = new[] { Page(1, true), Page(2, true) };
        var b = new[] { Page(1, true) };

        var result = ExperienceComparer.Compare(Lumber, a, Roofing, b);

        Assert.Equal(2, result.TotalPages);
        Assert.Equal(2, Assert.Single(result.OnlyA).PageId);
    }

    [Fact]
    public void ShortWhyLabels_TravelToBothSidesOfTheDifference()
    {
        var a = new[] { new ExperiencePage(1, 1, "Site", "Home", true, "full A", "Granted here") };
        var b = new[] { new ExperiencePage(1, 1, "Site", "Home", false, "full B", "Gated here") };

        var difference = Assert.Single(ExperienceComparer.Compare(Lumber, a, Roofing, b).OnlyA);

        Assert.Equal("Granted here", difference.ShortWhyA);
        Assert.Equal("Gated here", difference.ShortWhyB);
    }

    [Fact]
    public void ShortWhy_ForAnUnknownPage_SaysNotEvaluated()
    {
        var a = new[] { new ExperiencePage(1, 1, "Site", "Home", true, "full A", "Granted here") };

        var difference = Assert.Single(ExperienceComparer.Compare(Lumber, a, Roofing, []).OnlyA);

        Assert.Equal("Not evaluated", difference.ShortWhyB);
    }

    [Fact]
    public void EmptyBothSides_IsIdenticalAndEmpty()
    {
        var result = ExperienceComparer.Compare(Lumber, [], Roofing, []);

        Assert.True(result.Identical);
        Assert.Equal(0, result.TotalPages);
        Assert.Empty(result.Tallies);
    }

    // ---- Ordering and capping ---------------------------------------------------------------

    [Fact]
    public void DifferencesAreSortedByWebsiteThenPath()
    {
        var a = new[]
        {
            new ExperiencePage(1, 2, "Zebra site", "Home", true, ""),
            new ExperiencePage(2, 1, "Alpha site", "Zulu page", true, ""),
            new ExperiencePage(3, 1, "Alpha site", "Alpha page", true, "")
        };
        var b = a.Select(p => p with { Visible = false }).ToArray();

        var result = ExperienceComparer.Compare(Lumber, a, Roofing, b);

        Assert.Equal(
            ["Alpha page", "Zulu page", "Home"],
            result.OnlyA.Select(d => d.Path));
    }

    [Fact]
    public void Cap_TruncatesAndReportsWhatIsLeft()
    {
        var items = Enumerable.Range(1, 250).ToList();

        var (shown, hidden) = ExperienceComparer.Cap(items, 100);

        Assert.Equal(100, shown.Count);
        Assert.Equal(150, hidden);
    }

    [Fact]
    public void Cap_LeavesShortListsAlone()
    {
        var items = Enumerable.Range(1, 40).ToList();

        var (shown, hidden) = ExperienceComparer.Cap(items, 100);

        Assert.Equal(40, shown.Count);
        Assert.Equal(0, hidden);
    }

    [Fact]
    public void Cap_ZeroOrNegativeMeansNoCap()
    {
        var items = Enumerable.Range(1, 40).ToList();

        Assert.Equal(0, ExperienceComparer.Cap(items, 0).Hidden);
        Assert.Equal(40, ExperienceComparer.Cap(items, -5).Shown.Count);
    }

    [Fact]
    public void LabelsRoundTrip()
    {
        var result = ExperienceComparer.Compare(Lumber, [Page(1, true)], Roofing, [Page(1, false)]);

        Assert.Equal(Lumber, result.LabelA);
        Assert.Equal(Roofing, result.LabelB);
        Assert.False(result.BaselineMode);
    }
}
