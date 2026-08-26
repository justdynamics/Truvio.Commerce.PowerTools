using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Permissions;
using Truvio.Commerce.PowerTools.Core.Principals;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// Renders the Experience Analyzer report. Sections are HTML blocks rather than list grids:
/// every row carries a gate explanation, and the grid gives each column the same width and
/// clips long text.
/// </summary>
internal static class ExperienceReport
{
    public static ExperienceAnalyzerModel Build(
        ExperienceComparison comparison,
        SecurityAccount account,
        SecurityAccount other,
        bool comparing,
        bool baselineIsSelf,
        int areaId,
        string scopeName)
    {
        var model = new ExperienceAnalyzerModel
        {
            // The toolbar picker already shows the account — the title only repeats it when it
            // carries information (a picked account, a comparison). The anonymous default gets
            // a neutral tail instead of doubling the picker's label into the breadcrumb.
            Title = comparing
                ? $"{Short(account.DisplayName)} vs {Short(other.DisplayName)}"
                : string.Equals(account.Key, $"role:{SecurityAccount.AnonymousRole}", StringComparison.OrdinalIgnoreCase)
                    ? "Overview"
                    : Short(account.DisplayName),
            AccountName = account.DisplayName,
            CompareName = comparing ? other.DisplayName : string.Empty,
            Comparing = comparing,
            Scope = scopeName,
            VisibleA = $"{comparison.VisibleToA} of {comparison.TotalPages}",
            VisibleB = comparing || !baselineIsSelf ? $"{comparison.VisibleToB} of {comparison.TotalPages}" : string.Empty,
            DifferenceCount = comparison.DifferenceCount,
            Identical = comparison.Identical
        };

        if (baselineIsSelf)
        {
            model.Sections.Add(new ReportSectionModel
            {
                Heading = "Baseline",
                Html = SearchTables.Note(
                    "This IS the anonymous baseline — pick a second account from the toolbar to compare two experiences.")
            });
            model.Sections.Add(Websites(comparison, account, other, comparing: false, baselineIsSelf: true));
            return model;
        }

        model.Sections.Add(Headline(comparison, account, other, comparing));
        model.Sections.Add(Websites(comparison, account, other, comparing, baselineIsSelf: false));

        var onlyAHeading = comparing
            ? $"Only {account.DisplayName} sees"
            : "Exclusive to this account";
        var onlyBHeading = comparing
            ? $"Only {other.DisplayName} sees"
            : "Hidden from this account, public sees it";

        model.Sections.Add(Differences(onlyAHeading, comparison.OnlyA, account.Key, other.Key, showWhy: WhySide.B,
            empty: comparing
                ? $"No page is visible to {account.DisplayName} alone."
                : "This account sees nothing the public does not already see."));

        model.Sections.Add(Differences(onlyBHeading, comparison.OnlyB, account.Key, other.Key, showWhy: WhySide.A,
            empty: comparing
                ? $"No page is visible to {other.DisplayName} alone."
                : "Nothing public is hidden from this account."));

        return model;
    }

    private enum WhySide
    {
        /// <summary>Explain the A side (it is the side that does NOT see the page).</summary>
        A,

        /// <summary>Explain the B side.</summary>
        B
    }

    private static ReportSectionModel Headline(
        ExperienceComparison comparison, SecurityAccount account, SecurityAccount other, bool comparing)
    {
        string text;
        if (comparison.Identical)
        {
            text = comparing
                ? $"'{account.DisplayName}' and '{other.DisplayName}' see exactly the same {comparison.VisibleToA} page(s) — " +
                  "no page tells the two roles apart."
                : $"'{account.DisplayName}' sees exactly what the public sees ({comparison.VisibleToA} page(s)) — " +
                  "no page is gated to this account.";
        }
        else if (comparing)
        {
            text = $"{comparison.DifferenceCount} page(s) tell the two apart: " +
                   $"{comparison.OnlyA.Count} only '{account.DisplayName}' sees, " +
                   $"{comparison.OnlyB.Count} only '{other.DisplayName}' sees. " +
                   $"{comparison.Both.Count} page(s) are visible to both, {comparison.Neither.Count} to neither.";
        }
        else
        {
            text = $"{comparison.OnlyA.Count} page(s) are gated to this account and hidden from the public; " +
                   $"{comparison.OnlyB.Count} public page(s) are hidden from it. " +
                   $"{comparison.Both.Count} page(s) are visible to both, {comparison.Neither.Count} to neither.";
        }

        if (!comparing && comparison.OnlyB.Count > 0)
        {
            text += " A signed-in account seeing LESS than the public is rarely intended — check the rows below.";
        }

        return new ReportSectionModel { Heading = "Summary", Html = SearchTables.Note(text) };
    }

    private static ReportSectionModel Websites(
        ExperienceComparison comparison, SecurityAccount account, SecurityAccount other, bool comparing, bool baselineIsSelf)
    {
        var labelA = account.DisplayName;
        var labelB = comparing ? other.DisplayName : "Public (anonymous)";

        var headers = baselineIsSelf
            ? new List<string> { "Website", "Pages", labelA }
            : ["Website", "Pages", labelA, labelB, "Differs"];

        var rows = comparison.Tallies.Select(t =>
        {
            var differs = comparison.OnlyA.Count(d => d.AreaName == t.AreaName)
                          + comparison.OnlyB.Count(d => d.AreaName == t.AreaName);

            return baselineIsSelf
                ? new object?[] { new SearchTables.Wrap(t.AreaName), t.Total, t.VisibleA }
                : [
                    new SearchTables.Wrap(t.AreaName),
                    t.Total,
                    t.VisibleA,
                    t.VisibleB,
                    differs == 0 ? new SearchTables.Pill("Same", "ok") : new SearchTables.Pill(differs.ToString(), "warn")
                ];
        });

        return new ReportSectionModel { Heading = "Websites", Html = SearchTables.Table(headers, rows) };
    }

    private static ReportSectionModel Differences(
        string heading,
        IReadOnlyList<ExperienceDifference> differences,
        string accountKey,
        string compareKey,
        WhySide showWhy,
        string empty)
    {
        if (differences.Count == 0)
            return new ReportSectionModel { Heading = heading, Html = SearchTables.Note(empty) };

        var (shown, hidden) = ExperienceComparer.Cap(differences);

        var rows = shown.Select(d => new object?[]
        {
            new SearchTables.Wrap(d.AreaName),
            new SearchTables.Wrap(d.Path),
            new SearchTables.Link("Audit", AuditHref(showWhy == WhySide.A ? accountKey : compareKey, d.PageId)),
            new SearchTables.Wrap(showWhy == WhySide.A ? d.WhyA : d.WhyB)
        });

        var html = SearchTables.Table(["Website", "Page", string.Empty, "Why the other side does not see it"], rows);

        if (hidden > 0)
            html += SearchTables.Note($"{hidden} further page(s) not shown.");

        return new ReportSectionModel { Heading = $"{heading} ({differences.Count})", Html = html };
    }

    /// <summary>The Content Access Viewer's page drilldown for the account that is denied.</summary>
    private static string AuditHref(string accountKey, int pageId) =>
        "/Admin/UI/PowerTools/PageAudience" +
        $"?AccountKey={Uri.EscapeDataString(accountKey)}&PageId={pageId}" +
        "&Type=PageAudience&QueryContext=Dynamicweb.CoreUI.Data.DataQueryContext";

    /// <summary>Account names can be long; the title has to stay one line.</summary>
    private static string Short(string name) =>
        name.Length <= 22 ? name : name[..21].TrimEnd() + "…";
}
