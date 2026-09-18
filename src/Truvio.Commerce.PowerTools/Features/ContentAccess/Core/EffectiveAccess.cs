using Truvio.Commerce.PowerTools.Shared.Permissions;

namespace Truvio.Commerce.PowerTools.Features.ContentAccess.Core;

/// <summary>
/// The outcome of resolving one account against one content entity.
/// </summary>
/// <param name="Level">The effective permission level.</param>
/// <param name="Origin">Where the level came from.</param>
/// <param name="OriginPageId">Page whose explicit rows decided the outcome (self or ancestor), if any.</param>
/// <param name="WinningOwnerId">The owner id (role name or group id) whose contribution won, if any.</param>
public sealed record EffectiveAccess(
    int Level,
    AccessOrigin Origin,
    int? OriginPageId,
    string? WinningOwnerId)
{
    public bool GrantsRead => Levels.GrantsRead(Level);

    public string LevelName => Levels.Name(Level);
}
