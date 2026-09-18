namespace Truvio.Commerce.PowerTools.Features.ContentAccess.Core;

/// <summary>
/// One UnifiedPermission row on a content entity. OwnerId is a built-in role name
/// ("Anonymous", "AuthenticatedFrontend", ...) or a numeric user-group id.
/// </summary>
public sealed record ContentPermissionRow(string OwnerId, string EntityName, string Key, int Level);
