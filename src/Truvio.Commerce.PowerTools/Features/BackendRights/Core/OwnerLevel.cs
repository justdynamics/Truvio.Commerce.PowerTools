namespace Truvio.Commerce.PowerTools.Features.BackendRights.Core;

/// <summary>
/// One priority level of the backend permission-owner chain, in resolution order: the user's direct
/// groups, then their ancestors level by level, then the backend roles. The user's own id is never
/// an owner in the backend.
/// </summary>
public sealed record OwnerLevel(int Level, string Description, IReadOnlyList<OwnerSpec> Owners);
