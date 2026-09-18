namespace Truvio.Commerce.PowerTools.Features.BackendRights.Core;

/// <param name="DefaultLevel">
/// The owner's <c>DefaultPermission</c>, or null when it declares none —
/// <c>AuthenticatedBackend</c>'s null default is why backend access is grant-only.
/// </param>
public sealed record OwnerSpec(string Id, string DisplayName, string Kind, int? DefaultLevel);
