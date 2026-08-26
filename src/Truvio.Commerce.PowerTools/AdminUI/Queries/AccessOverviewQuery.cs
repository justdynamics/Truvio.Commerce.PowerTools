using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Diagnostics;
using Truvio.Commerce.PowerTools.Core.Diagnostics.Rules;
using Truvio.Commerce.PowerTools.Core.Permissions;
using Truvio.Commerce.PowerTools.Core.Permissions.Dw;
using Truvio.Commerce.PowerTools.Core.Principals;
using Truvio.Commerce.PowerTools.Core.Principals.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// Account -> content tree: every page across all websites, in tree order with indentation,
/// with the effective level, its origin, and any gating warnings on that page.
/// AccountKey carries the account ("role:Anonymous" / "group:42" / "user:17").
/// </summary>
public sealed class AccessOverviewQuery : DataQueryListBase<AccessNodeModel, AccessNodeModel, DataListViewModel<AccessNodeModel>>
{
    public string AccountKey { get; set; } = string.Empty;

    /// <summary>Resolves a toolbar slide-over pick — see <see cref="PickStore"/>.</summary>
    public string PickToken { get; set; } = string.Empty;

    /// <summary>0 = every website; otherwise only that area's pages.</summary>
    public int AreaId { get; set; }

    /// <summary>
    /// The tool starts directly on this screen: with nothing selected it shows the built-in
    /// anonymous frontend role across all websites, and the toolbar pickers take it from
    /// there — no full-screen account list up front.
    /// </summary>
    internal static string DefaultAccountKey()
    {
        try
        {
            return new DwAccountCatalog().GetRoles().FirstOrDefault()?.Key ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    protected override IEnumerable<AccessNodeModel>? GetListItems()
    {
        if (!string.IsNullOrEmpty(PickToken) && PickStore.Get(PickToken) is { Length: > 0 } picked)
            AccountKey = picked;

        if (string.IsNullOrEmpty(AccountKey))
            AccountKey = DefaultAccountKey();

        var account = new DwAccountCatalog().Resolve(AccountKey);
        if (account is null)
            return [];

        var source = new DwContentSecuritySource();
        var evaluator = new EffectiveAccessEvaluator(source);
        var ownerName = OwnerNameResolver();

        // Per-page gating warnings (the personalisation traps) for the badge column.
        var warningsByPageKey = new BareGroupGrantRule()
            .Evaluate(new WarningContext(source))
            .Where(f => f.EntityName == ContentEntityNames.Page)
            .ToLookup(f => f.EntityKey, f => f.Title);

        var items = new List<AccessNodeModel>();

        foreach (var area in source.GetAreas().OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (AreaId > 0 && area.Id != AreaId)
                continue;

            var pages = source.GetPages(area.Id);
            var pagesById = pages.ToDictionary(p => p.Id);
            var byParent = pages.ToLookup(p => p.ParentPageId);

            items.Add(new AccessNodeModel
            {
                PageId = 0,
                AccountKey = account.Key,
                Name = $"\U0001F310 {area.Name}",
                Visible = string.Empty,
                Level = string.Empty,
                Origin = "Website",
                VisibleState = null,
                LevelValue = -1
            });

            AddPages(parentId: 0, depth: 1);
            continue;

            void AddPages(int parentId, int depth)
            {
                foreach (var page in byParent[parentId].OrderBy(p => p.Sort))
                {
                    var access = evaluator.EvaluatePage(account, page.Id, pagesById);
                    items.Add(new AccessNodeModel
                    {
                        PageId = page.Id,
                        AccountKey = account.Key,
                        Name = string.Concat(Enumerable.Repeat(" ", depth)) + page.Name,
                        Visible = access.GrantsRead ? "Yes" : "No",
                        Level = access.LevelName,
                        Origin = Explain(account, access, evaluator, pagesById, ownerName),
                        Warning = string.Join("; ", warningsByPageKey[page.Id.ToString()]),
                        VisibleState = access.GrantsRead,
                        LevelValue = access.Level,
                        OriginKind = access.Origin.ToString()
                    });
                    AddPages(page.Id, depth + 1);
                }
            }
        }

        return items;
    }

    /// <summary>The page-level explanation: rows come from the page the verdict originated on.</summary>
    internal static string Explain(
        SecurityAccount account,
        EffectiveAccess access,
        EffectiveAccessEvaluator evaluator,
        IReadOnlyDictionary<int, PageNode> pagesById,
        Func<string?, string> ownerName)
    {
        var rows = access.OriginPageId is int pid ? evaluator.GetExplicitPageRows(pid) : [];
        var originName = access.Origin == AccessOrigin.InheritedFromPage && access.OriginPageId is int origin
            ? pagesById.TryGetValue(origin, out var p) ? p.Name : origin.ToString()
            : null;
        return AccessExplanation.Explain(account, access, rows, originName, ownerName);
    }

    /// <summary>The compact gate label ("Gated here", "Gated on 'Account'") for dense tables.</summary>
    internal static string ShortExplain(EffectiveAccess access, IReadOnlyDictionary<int, PageNode> pagesById)
    {
        var originName = access.Origin == AccessOrigin.InheritedFromPage && access.OriginPageId is int origin
            ? pagesById.TryGetValue(origin, out var p) ? p.Name : origin.ToString()
            : null;
        return AccessExplanation.Short(access, originName);
    }

    /// <summary>Owner ids as people know them: role names and actual group names, not "group 60".</summary>
    internal static Func<string?, string> OwnerNameResolver()
    {
        Dictionary<string, string>? groups = null;
        return ownerId =>
        {
            switch (ownerId)
            {
                case null:
                    return "none";
                case SecurityAccount.AnonymousRole:
                    return "Anonymous role";
                case SecurityAccount.AuthenticatedFrontendRole:
                    return "Authenticated frontend role";
            }

            if (int.TryParse(ownerId, out _))
            {
                try
                {
                    groups ??= new DwAccountCatalog().GetGroups()
                        .GroupBy(g => g.Id).ToDictionary(g => g.Key, g => g.First().DisplayName, StringComparer.Ordinal);
                }
                catch
                {
                    groups = [];
                }

                return groups.TryGetValue(ownerId, out var name) ? name : $"group {ownerId}";
            }

            return ownerId;
        };
    }

    protected override IEnumerable<AccessNodeModel> MapModels(IEnumerable<AccessNodeModel> items) => items;

    protected override DataListViewModel<AccessNodeModel> MakeListModel() => new();
}
