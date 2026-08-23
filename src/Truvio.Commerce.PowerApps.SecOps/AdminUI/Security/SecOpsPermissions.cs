using Dynamicweb.Security.Permissions;

namespace Truvio.Commerce.PowerApps.SecOps.AdminUI.Security;

/// <summary>
/// DW unified-permission entity for the SecOps tools. Registering the entity + lookup makes
/// the Security Viewer a first-class permission target: admins grant or deny it per
/// user/group through DW's standard permission screen, exactly like content permissions.
/// Per DW semantics the function is open until an admin explicitly manages it; built-in
/// admins are always elevated.
/// </summary>
public sealed class SecOpsPermissionEntity : IPermissionEntity
{
    /// <summary>Lookup name — the "entity type" under which DW stores and resolves grants.</summary>
    public const string PermissionName = "Truvio PowerApps SecOps";

    public const string SecurityViewerKey = "truvio-secops-security-viewer";

    private readonly string _key;

    public SecOpsPermissionEntity(string key) => _key = key;

    public string GetPermissionKey() => _key;

    public IEnumerable<IPermissionEntity> GetPermissionParents() => Enumerable.Empty<IPermissionEntity>();
}

/// <summary>Resolves stored permission keys back to entities — auto-discovered by DW's
/// AddInManager (see PermissionEntityLookupManager).</summary>
public sealed class SecOpsPermissionEntityLookup : IPermissionEntityLookup
{
    public string PermissionName => SecOpsPermissionEntity.PermissionName;

    public IPermissionEntity? GetPermissionEntityByKey(string key) =>
        key is SecOpsPermissionEntity.SecurityViewerKey
            ? new SecOpsPermissionEntity(key)
            : null;
}

/// <summary>
/// Access checks for the SecOps tools. The viewer is read-only, so Read on the function
/// grant is the only requirement. Checks fail CLOSED — an exception during evaluation
/// denies access.
/// </summary>
public static class SecOpsAccess
{
    public static bool CanUseSecurityViewer()
    {
        try
        {
            return new SecOpsPermissionEntity(SecOpsPermissionEntity.SecurityViewerKey)
                .GetPermission()
                .HasPermission(PermissionLevel.Read);
        }
        catch
        {
            return false;
        }
    }
}
