namespace Truvio.Commerce.PowerApps.SecOps.Core.Permissions;

public enum AccessOrigin
{
    /// <summary>Account bypasses all permission checks (Angel / built-in admin / Administrator).</summary>
    Bypass,

    /// <summary>An explicit row on the entity itself decided the level.</summary>
    ExplicitHere,

    /// <summary>An explicit row on an ancestor page decided the level (page inheritance).</summary>
    InheritedFromPage,

    /// <summary>No explicit row applied to any of the account's identities; a frontend role default won.</summary>
    RoleDefault,

    /// <summary>Grid row / paragraph carries no rows of its own; the page outcome applies.</summary>
    PageFallback
}

/// <summary>
/// The outcome of resolving one account against one content entity.
/// </summary>
public sealed record EffectiveAccess(
    int Level,
    AccessOrigin Origin,
    /// <summary>Page whose explicit rows decided the outcome (self or ancestor), if any.</summary>
    int? OriginPageId,
    /// <summary>The owner id (role name or group id) whose contribution won, if any.</summary>
    string? WinningOwnerId)
{
    public bool GrantsRead => Levels.GrantsRead(Level);

    public string LevelName => Levels.Name(Level);
}
