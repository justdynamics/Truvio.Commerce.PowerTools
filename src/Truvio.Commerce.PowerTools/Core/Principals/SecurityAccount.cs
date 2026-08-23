namespace Truvio.Commerce.PowerTools.Core.Principals;

public enum SecurityAccountKind
{
    Role,
    Group,
    User
}

/// <summary>
/// A security account the viewer evaluates content access for: a built-in frontend role,
/// a user group, or an individual user. The account key round-trips through screen queries
/// as "role:Anonymous", "group:42", or "user:17".
/// </summary>
public sealed class SecurityAccount
{
    public const string AnonymousRole = "Anonymous";
    public const string AuthenticatedFrontendRole = "AuthenticatedFrontend";

    public SecurityAccountKind Kind { get; init; }

    /// <summary>Role name for roles; numeric id (as string) for groups and users.</summary>
    public string Id { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    /// <summary>User accounts only: the e-mail address (surfaced so searches by e-mail work).</summary>
    public string? Email { get; init; }

    /// <summary>
    /// The UnifiedPermission owner ids this account resolves through (highest level wins).
    /// A role is just itself. A group is itself plus AuthenticatedFrontend (a member is by
    /// definition an authenticated frontend visitor). A user is AuthenticatedFrontend plus
    /// every group the user belongs to — DW ignores per-user grants at render time, so the
    /// user's own id is deliberately NOT part of the set.
    /// </summary>
    public IReadOnlyList<string> OwnerIds { get; init; } = [];

    /// <summary>
    /// True for accounts that bypass every permission check (Angel, built-in admin,
    /// Administrator user type). The evaluator reports full access without consulting rows.
    /// </summary>
    public bool BypassesChecks { get; init; }

    public string Key => Kind switch
    {
        SecurityAccountKind.Role => $"role:{Id}",
        SecurityAccountKind.Group => $"group:{Id}",
        _ => $"user:{Id}"
    };

    public static (SecurityAccountKind Kind, string Id)? ParseKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;
        var idx = key.IndexOf(':');
        if (idx <= 0 || idx == key.Length - 1)
            return null;
        var id = key[(idx + 1)..];
        return key[..idx].ToLowerInvariant() switch
        {
            "role" => (SecurityAccountKind.Role, id),
            "group" => (SecurityAccountKind.Group, id),
            "user" => (SecurityAccountKind.User, id),
            _ => null
        };
    }
}
