using Dynamicweb.Security.UserManagement;

namespace Truvio.Commerce.PowerTools.Core.Principals.Dw;

/// <summary>
/// Builds <see cref="SecurityAccount"/> descriptors from the live DW user store.
/// </summary>
public sealed class DwAccountCatalog
{
    public IReadOnlyList<SecurityAccount> GetRoles() =>
    [
        new SecurityAccount
        {
            Kind = SecurityAccountKind.Role,
            Id = SecurityAccount.AnonymousRole,
            DisplayName = "Anonymous users (frontend)",
            OwnerIds = [SecurityAccount.AnonymousRole]
        },
        new SecurityAccount
        {
            Kind = SecurityAccountKind.Role,
            Id = SecurityAccount.AuthenticatedFrontendRole,
            DisplayName = "Authenticated users (frontend)",
            OwnerIds = [SecurityAccount.AuthenticatedFrontendRole]
        }
    ];

    public IReadOnlyList<SecurityAccount> GetGroups()
    {
        var groups = UserManagementServices.UserGroups.GetGroups().ToList();
        var byId = groups.ToDictionary(g => g.ID);
        return groups
            .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => BuildGroupAccount(g, byId))
            .ToList();
    }

    public (IReadOnlyList<SecurityAccount> Users, int TotalCount) GetUsers(string? search, int pageSize = 100)
    {
        var result = UserManagementServices.Users.GetUsersBySearch(new UserSearchFilter
        {
            SearchValue = search ?? string.Empty,
            PageNumber = 1,
            PageSize = pageSize
        });

        // One group index shared across all users (BuildUserAccount walks ancestor chains).
        var byId = UserManagementServices.UserGroups.GetGroups().ToDictionary(g => g.ID);
        var users = result.Users
            .Select(u => BuildUserAccount(u, byId))
            .ToList();
        return (users, result.TotalCount);
    }

    public SecurityAccount? Resolve(string? accountKey)
    {
        var parsed = SecurityAccount.ParseKey(accountKey);
        if (parsed is null)
            return null;

        var (kind, id) = parsed.Value;
        switch (kind)
        {
            case SecurityAccountKind.Role:
                return GetRoles().FirstOrDefault(r => string.Equals(r.Id, id, StringComparison.OrdinalIgnoreCase));

            case SecurityAccountKind.Group:
            {
                if (!int.TryParse(id, out var groupId))
                    return null;
                var group = UserManagementServices.UserGroups.GetGroupById(groupId);
                if (group is null)
                    return null;
                var byId = UserManagementServices.UserGroups.GetGroups().ToDictionary(g => g.ID);
                return BuildGroupAccount(group, byId);
            }

            default:
            {
                if (!int.TryParse(id, out var userId))
                    return null;
                var user = UserManagementServices.Users.GetUserById(userId);
                if (user is null)
                    return null;
                var byId = UserManagementServices.UserGroups.GetGroups().ToDictionary(g => g.ID);
                return BuildUserAccount(user, byId);
            }
        }
    }

    /// <summary>
    /// A group member is by definition an authenticated frontend visitor, and grants placed on
    /// ancestor groups cover the nested groups beneath them — so the owner set is the group
    /// itself, its ancestor chain, and the AuthenticatedFrontend role.
    /// </summary>
    private static SecurityAccount BuildGroupAccount(UserGroup group, IReadOnlyDictionary<int, UserGroup> byId)
    {
        var owners = new List<string> { SecurityAccount.AuthenticatedFrontendRole };
        owners.AddRange(SelfAndAncestors(group.ID, byId).Select(id => id.ToString()));

        return new SecurityAccount
        {
            Kind = SecurityAccountKind.Group,
            Id = group.ID.ToString(),
            DisplayName = group.Name,
            OwnerIds = owners
        };
    }

    private static SecurityAccount BuildUserAccount(User user, IReadOnlyDictionary<int, UserGroup> byId)
    {
        var owners = new List<string> { SecurityAccount.AuthenticatedFrontendRole };
        owners.AddRange(user.GetGroups()
            .SelectMany(g => SelfAndAncestors(g.ID, byId))
            .Distinct()
            .Select(id => id.ToString()));

        return new SecurityAccount
        {
            Kind = SecurityAccountKind.User,
            Id = user.ID.ToString(),
            DisplayName = string.IsNullOrEmpty(user.Name) ? user.UserName : $"{user.Name} ({user.UserName})",
            Email = user.Email,
            OwnerIds = owners,
            BypassesChecks = user.IsAngel || user.IsBuiltInAdmin || user.IsAdmin
        };
    }

    private static IEnumerable<int> SelfAndAncestors(int groupId, IReadOnlyDictionary<int, UserGroup> byId)
    {
        var current = groupId;
        var guard = 0;
        while (current > 0 && guard++ < 50)
        {
            yield return current;
            current = byId.TryGetValue(current, out var g) ? g.ParentGroupID : 0;
        }
    }
}
