using Truvio.Commerce.PowerTools.Core.Principals;

namespace Truvio.Commerce.PowerTools.Core.Permissions;

/// <summary>
/// The human explanation behind an <see cref="EffectiveAccess"/> verdict. The naked
/// "winner: X" wording confused exactly the case it mattered most: a gated page (broad
/// role → None plus a grant for one group) shows "winner: Authenticated frontend role" for a
/// denied group and never says WHY membership of an authenticated group does not help. This
/// spells it out: which row won, that the account has no grant of its own, and who IS
/// granted — pure composition, testable without Dynamicweb.
/// </summary>
public static class AccessExplanation
{
    private const int MaxGrantedListed = 4;

    /// <param name="account">The account the verdict was computed for.</param>
    /// <param name="access">The verdict.</param>
    /// <param name="originRows">The explicit rows of the entity the verdict came from (empty when none apply).</param>
    /// <param name="originPageName">The ancestor page the rows live on, when inherited; null when they sit on the entity itself.</param>
    /// <param name="ownerName">Renders an owner id ("Anonymous", "60") as a display name.</param>
    public static string Explain(
        SecurityAccount account,
        EffectiveAccess access,
        IReadOnlyList<ContentPermissionRow> originRows,
        string? originPageName,
        Func<string?, string> ownerName)
    {
        switch (access.Origin)
        {
            case AccessOrigin.Bypass:
                return "Administrator - bypasses permissions";
            case AccessOrigin.RoleDefault:
                return $"Role default ({ownerName(access.WinningOwnerId)})";
            case AccessOrigin.PageFallback:
                return "Follows the page";
        }

        var where = originPageName is null ? "here" : $"on '{originPageName}'";

        if (access.GrantsRead)
            return $"Set {where}: '{ownerName(access.WinningOwnerId)}' grants {access.LevelName}.";

        // Denied by explicit rows. Say why membership does not help, and who does get in.
        var ownRow = originRows.FirstOrDefault(row =>
            !IsFrontendRole(row.OwnerId) &&
            account.OwnerIds.Contains(row.OwnerId, StringComparer.OrdinalIgnoreCase));

        var granted = originRows
            .Where(row => Levels.GrantsRead(row.Level))
            .Select(row => $"'{ownerName(row.OwnerId)}'")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        string reason;
        if (ownRow is not null && !Levels.GrantsRead(ownRow.Level))
        {
            reason = $"'{account.DisplayName}' is explicitly set to {Levels.Name(ownRow.Level)} {where}.";
        }
        else if (access.WinningOwnerId is not null)
        {
            reason = $"Gated {where}: '{ownerName(access.WinningOwnerId)}' is set to " +
                     $"{access.LevelName} and '{account.DisplayName}' has no grant of its own.";
        }
        else
        {
            reason = $"Gated {where}: none of the explicit permissions applies to '{account.DisplayName}'.";
        }

        if (granted.Count == 0)
            return reason + " Nothing is granted - only administrators see this.";

        var listed = string.Join(", ", granted.Take(MaxGrantedListed));
        var more = granted.Count > MaxGrantedListed ? $" and {granted.Count - MaxGrantedListed} more" : string.Empty;
        return $"{reason} Only {listed}{more} can see it.";
    }

    private static bool IsFrontendRole(string ownerId) =>
        string.Equals(ownerId, SecurityAccount.AnonymousRole, StringComparison.OrdinalIgnoreCase)
        || string.Equals(ownerId, SecurityAccount.AuthenticatedFrontendRole, StringComparison.OrdinalIgnoreCase);
}
