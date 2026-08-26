using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Permissions;
using Truvio.Commerce.PowerTools.Core.Permissions.Dw;
using Truvio.Commerce.PowerTools.Core.Principals;
using Truvio.Commerce.PowerTools.Core.Principals.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// The Experience Analyzer report: what one account's content experience looks like, and how
/// it differs from another account's — the answer to "the Lumber role sees these pages, the
/// Roofing role sees those, and here is what gates each one".
/// <para>
/// With no comparison account chosen the anonymous role is the baseline, so a single account
/// still has something to stand out against.
/// </para>
/// </summary>
public sealed class ExperienceAnalyzerQuery : DataQueryModelBase<ExperienceAnalyzerModel>
{
    /// <summary>"role:Anonymous" / "group:42" / "user:17".</summary>
    public string AccountKey { get; set; } = string.Empty;

    /// <summary>The account to compare against; empty = the anonymous baseline.</summary>
    public string CompareKey { get; set; } = string.Empty;

    /// <summary>0 = every website; otherwise only that area's pages.</summary>
    public int AreaId { get; set; }

    /// <summary>Resolves a toolbar pick for the first account — see <see cref="PickStore"/>.</summary>
    public string PickToken { get; set; } = string.Empty;

    /// <summary>Resolves a toolbar pick for the comparison account.</summary>
    public string ComparePickToken { get; set; } = string.Empty;

    /// <summary>The account keys in effect, pick tokens resolved. Used by the screen and the toolbar.</summary>
    internal (string AccountKey, string CompareKey) EffectiveKeys()
    {
        var accountKey = AccountKey;
        if (!string.IsNullOrEmpty(PickToken) && PickStore.Get(PickToken) is { Length: > 0 } picked)
            accountKey = picked;
        if (string.IsNullOrEmpty(accountKey))
            accountKey = AccessOverviewQuery.DefaultAccountKey();

        var compareKey = CompareKey;
        if (!string.IsNullOrEmpty(ComparePickToken) && PickStore.Get(ComparePickToken) is { Length: > 0 } pickedB)
            compareKey = pickedB;

        // Comparing an account with itself says nothing — treat it as single-account mode.
        if (!string.IsNullOrEmpty(compareKey) && string.Equals(compareKey, accountKey, StringComparison.OrdinalIgnoreCase))
            compareKey = string.Empty;

        return (accountKey, compareKey);
    }

    public override ExperienceAnalyzerModel? GetModel()
    {
        var (accountKey, compareKey) = EffectiveKeys();
        AccountKey = accountKey;
        CompareKey = compareKey;

        try
        {
            var catalog = new DwAccountCatalog();
            var account = catalog.Resolve(accountKey);
            if (account is null)
                return new ExperienceAnalyzerModel { Title = "Experience Analyzer", Error = "No account selected." };

            var comparing = !string.IsNullOrEmpty(compareKey);
            var other = comparing
                ? catalog.Resolve(compareKey)
                : catalog.Resolve($"role:{SecurityAccount.AnonymousRole}");

            if (other is null)
                return new ExperienceAnalyzerModel
                {
                    Title = account.DisplayName,
                    Error = comparing ? "The comparison account no longer exists." : "The anonymous role could not be resolved."
                };

            // The account IS the baseline: there is nothing to stand out against.
            var baselineIsSelf = !comparing && string.Equals(account.Key, other.Key, StringComparison.OrdinalIgnoreCase);

            var source = new DwContentSecuritySource();
            var pagesA = Evaluate(source, account, AreaId);
            var pagesB = baselineIsSelf ? [] : Evaluate(source, other, AreaId);

            var comparison = ExperienceComparer.Compare(
                account.DisplayName,
                pagesA,
                other.DisplayName,
                pagesB,
                baselineMode: !comparing);

            return ExperienceReport.Build(comparison, account, other, comparing, baselineIsSelf, AreaId, ScopeName(source, AreaId));
        }
        catch (Exception ex)
        {
            return new ExperienceAnalyzerModel { Title = "Experience Analyzer", Error = ex.Message };
        }
    }

    /// <summary>Every page in scope as one account experiences it, with the gate explanation.</summary>
    internal static IReadOnlyList<ExperiencePage> Evaluate(IContentSecuritySource source, SecurityAccount account, int areaId)
    {
        var evaluator = new EffectiveAccessEvaluator(source);
        var ownerName = AccessOverviewQuery.OwnerNameResolver();
        var pages = new List<ExperiencePage>();

        foreach (var area in source.GetAreas().OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (areaId > 0 && area.Id != areaId)
                continue;

            var areaPages = source.GetPages(area.Id);
            var pagesById = areaPages.ToDictionary(p => p.Id);

            foreach (var page in areaPages)
            {
                var access = evaluator.EvaluatePage(account, page.Id, pagesById);
                pages.Add(new ExperiencePage(
                    page.Id,
                    area.Id,
                    area.Name,
                    Path(page, pagesById),
                    access.GrantsRead,
                    AccessOverviewQuery.Explain(account, access, evaluator, pagesById, ownerName)));
            }
        }

        return pages;
    }

    /// <summary>"Home / Dashboards / Lumber" — the trail inside its website, parents first.</summary>
    private static string Path(PageNode page, IReadOnlyDictionary<int, PageNode> pagesById)
    {
        var parts = new List<string> { page.Name };
        var currentId = page.ParentPageId;
        var guard = 0;
        while (currentId > 0 && guard++ < 50 && pagesById.TryGetValue(currentId, out var parent))
        {
            parts.Add(parent.Name);
            currentId = parent.ParentPageId;
        }

        parts.Reverse();
        return string.Join(" / ", parts);
    }

    private static string ScopeName(IContentSecuritySource source, int areaId)
    {
        if (areaId <= 0)
            return "All websites";

        try
        {
            return source.GetAreas().FirstOrDefault(a => a.Id == areaId)?.Name ?? $"Website {areaId}";
        }
        catch
        {
            return $"Website {areaId}";
        }
    }
}
