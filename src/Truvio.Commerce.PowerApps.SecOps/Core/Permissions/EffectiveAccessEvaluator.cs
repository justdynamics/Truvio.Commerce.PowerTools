using Truvio.Commerce.PowerApps.SecOps.Core.Principals;

namespace Truvio.Commerce.PowerApps.SecOps.Core.Permissions;

/// <summary>
/// Mirrors DW10's render-time content permission resolution for an arbitrary account:
///
/// 1. Each identity of the account (role names + group ids) contributes a level: its explicit
///    row on the resolved entity if one exists, otherwise its role default (frontend roles
///    default to Read; groups have no default). The highest contribution wins.
/// 2. A page carrying no explicit rows inherits from the nearest ancestor page that carries
///    rows; with none anywhere, only role defaults apply.
/// 3. Grid rows and paragraphs carrying no rows of their own fall back to the page outcome.
///
/// Per-user rows are ignored by the runtime, so <see cref="SecurityAccount.OwnerIds"/> never
/// includes a user id.
/// </summary>
public sealed class EffectiveAccessEvaluator
{
    private readonly ILookup<string, ContentPermissionRow> _pageRows;
    private readonly ILookup<string, ContentPermissionRow> _gridRowRows;
    private readonly ILookup<string, ContentPermissionRow> _paragraphRows;

    public EffectiveAccessEvaluator(IContentSecuritySource source)
    {
        _pageRows = source.GetRows(ContentEntityNames.Page).ToLookup(r => r.Key);
        _gridRowRows = source.GetRows(ContentEntityNames.GridRow).ToLookup(r => r.Key);
        _paragraphRows = source.GetRows(ContentEntityNames.Paragraph).ToLookup(r => r.Key);
    }

    /// <summary>Explicit rows carried by a page (empty when the page inherits).</summary>
    public IReadOnlyList<ContentPermissionRow> GetExplicitPageRows(int pageId) =>
        _pageRows[pageId.ToString()].ToList();

    public EffectiveAccess EvaluatePage(SecurityAccount account, int pageId, IReadOnlyDictionary<int, PageNode> pagesById)
    {
        if (account.BypassesChecks)
            return new EffectiveAccess(Levels.All, AccessOrigin.Bypass, null, null);

        // Walk self -> ancestors to the nearest page carrying explicit rows.
        var currentId = pageId;
        var guard = 0;
        while (currentId > 0 && guard++ < 200)
        {
            var rows = _pageRows[currentId.ToString()].ToList();
            if (rows.Count > 0)
            {
                var (level, winner, isDefault) = Resolve(rows, account);
                var origin = isDefault
                    ? AccessOrigin.RoleDefault
                    : currentId == pageId ? AccessOrigin.ExplicitHere : AccessOrigin.InheritedFromPage;
                return new EffectiveAccess(level, origin, currentId, winner);
            }

            if (!pagesById.TryGetValue(currentId, out var node) || node.ParentPageId <= 0)
                break;
            currentId = node.ParentPageId;
        }

        // No explicit rows anywhere on the chain: role defaults only.
        var (defaultLevel, defaultWinner, _) = Resolve([], account);
        return new EffectiveAccess(defaultLevel, AccessOrigin.RoleDefault, null, defaultWinner);
    }

    public EffectiveAccess EvaluateGridRow(SecurityAccount account, int gridRowId, EffectiveAccess pageAccess) =>
        EvaluateChild(account, _gridRowRows, gridRowId, pageAccess);

    public EffectiveAccess EvaluateParagraph(SecurityAccount account, int paragraphId, EffectiveAccess pageAccess) =>
        EvaluateChild(account, _paragraphRows, paragraphId, pageAccess);

    private static EffectiveAccess EvaluateChild(
        SecurityAccount account,
        ILookup<string, ContentPermissionRow> rowsByKey,
        int entityId,
        EffectiveAccess pageAccess)
    {
        if (account.BypassesChecks)
            return new EffectiveAccess(Levels.All, AccessOrigin.Bypass, null, null);

        var rows = rowsByKey[entityId.ToString()].ToList();
        if (rows.Count == 0)
            return pageAccess with { Origin = AccessOrigin.PageFallback };

        var (level, winner, isDefault) = Resolve(rows, account);
        return new EffectiveAccess(
            level,
            isDefault ? AccessOrigin.RoleDefault : AccessOrigin.ExplicitHere,
            pageAccess.OriginPageId,
            winner);
    }

    /// <summary>
    /// Highest-wins resolution over the account's identities. Returns the winning level, the
    /// owner id that produced it (null when nothing contributed), and whether the winner was
    /// a role default rather than an explicit row.
    /// </summary>
    private static (int Level, string? WinningOwnerId, bool WasDefault) Resolve(
        IReadOnlyList<ContentPermissionRow> rows,
        SecurityAccount account)
    {
        var best = Levels.NotSet;
        string? winner = null;
        var wasDefault = false;

        foreach (var ownerId in account.OwnerIds)
        {
            var row = rows.FirstOrDefault(r => string.Equals(r.OwnerId, ownerId, StringComparison.OrdinalIgnoreCase));
            int contribution;
            bool contributionIsDefault;
            if (row is not null)
            {
                contribution = row.Level;
                contributionIsDefault = false;
            }
            else if (IsFrontendRole(ownerId))
            {
                // Frontend roles default to Read when they carry no explicit row.
                contribution = Levels.Read;
                contributionIsDefault = true;
            }
            else
            {
                continue; // groups contribute nothing without an explicit row
            }

            if (contribution > best)
            {
                best = contribution;
                winner = ownerId;
                wasDefault = contributionIsDefault;
            }
        }

        return (best, winner, wasDefault);
    }

    private static bool IsFrontendRole(string ownerId) =>
        string.Equals(ownerId, SecurityAccount.AnonymousRole, StringComparison.OrdinalIgnoreCase)
        || string.Equals(ownerId, SecurityAccount.AuthenticatedFrontendRole, StringComparison.OrdinalIgnoreCase);
}
