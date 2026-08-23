using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Diagnostics;
using Truvio.Commerce.PowerTools.Core.Diagnostics.Rules;
using Truvio.Commerce.PowerTools.Core.Permissions;
using Truvio.Commerce.PowerTools.Core.Permissions.Dw;
using Truvio.Commerce.PowerTools.Core.Principals.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// Account -> content tree: every page across all websites, in tree order with indentation,
/// with the effective level, its origin, and any gating warnings on that page.
/// AccountKey carries the account ("role:Anonymous" / "group:42" / "user:17").
/// </summary>
public sealed class AccessOverviewQuery : DataQueryModelBase<DataListViewModel<AccessNodeModel>>
{
    public string AccountKey { get; set; } = string.Empty;

    public override DataListViewModel<AccessNodeModel>? GetModel()
    {
        var account = new DwAccountCatalog().Resolve(AccountKey);
        if (account is null)
            return new DataListViewModel<AccessNodeModel>();

        var source = new DwContentSecuritySource();
        var evaluator = new EffectiveAccessEvaluator(source);

        // Per-page gating warnings (the personalisation traps) for the badge column.
        var warningsByPageKey = new BareGroupGrantRule()
            .Evaluate(new WarningContext(source))
            .Where(f => f.EntityName == ContentEntityNames.Page)
            .ToLookup(f => f.EntityKey, f => f.Title);

        var items = new List<AccessNodeModel>();

        foreach (var area in source.GetAreas().OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
        {
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
                        Origin = Describe(access, pagesById),
                        Warning = string.Join("; ", warningsByPageKey[page.Id.ToString()]),
                        VisibleState = access.GrantsRead,
                        LevelValue = access.Level,
                        OriginKind = access.Origin.ToString()
                    });
                    AddPages(page.Id, depth + 1);
                }
            }
        }

        return new DataListViewModel<AccessNodeModel>
        {
            Data = items,
            TotalCount = items.Count
        };
    }

    internal static string Describe(EffectiveAccess access, IReadOnlyDictionary<int, PageNode> pagesById) =>
        access.Origin switch
        {
            AccessOrigin.Bypass => "Administrator - bypasses permissions",
            AccessOrigin.ExplicitHere => $"Set here (winner: {DescribeOwner(access.WinningOwnerId)})",
            AccessOrigin.InheritedFromPage when access.OriginPageId is int pid =>
                $"Inherited from '{(pagesById.TryGetValue(pid, out var p) ? p.Name : pid.ToString())}' "
                + $"(winner: {DescribeOwner(access.WinningOwnerId)})",
            AccessOrigin.RoleDefault => $"Role default ({DescribeOwner(access.WinningOwnerId)})",
            AccessOrigin.PageFallback => "Follows the page",
            _ => access.Origin.ToString()
        };

    internal static string DescribeOwner(string? ownerId) => ownerId switch
    {
        null => "none",
        "Anonymous" => "Anonymous role",
        "AuthenticatedFrontend" => "Authenticated frontend role",
        _ when int.TryParse(ownerId, out _) => $"group {ownerId}",
        _ => ownerId
    };
}
