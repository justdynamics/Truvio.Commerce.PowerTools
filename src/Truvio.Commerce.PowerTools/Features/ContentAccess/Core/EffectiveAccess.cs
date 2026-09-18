using Truvio.Commerce.PowerTools.Shared.Permissions;

namespace Truvio.Commerce.PowerTools.Features.ContentAccess.Core;

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
