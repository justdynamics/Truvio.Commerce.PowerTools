using Dynamicweb.Security.Permissions;

namespace Truvio.Commerce.PowerTools.AdminUI.Security;

/// <summary>
/// DW unified-permission entity for the PowerTools suite. Registering the entity + lookup makes
/// the Security Viewer a first-class permission target: admins grant or deny it per
/// user/group through DW's standard permission screen, exactly like content permissions.
/// Per DW semantics the function is open until an admin explicitly manages it; built-in
/// admins are always elevated.
/// </summary>
public sealed class PowerToolsPermissionEntity : IPermissionEntity
{
    /// <summary>Lookup name — the "entity type" under which DW stores and resolves grants.</summary>
    public const string PermissionName = "Truvio PowerTools";

    public const string SecurityViewerKey = "truvio-powertools-security-viewer";

    private readonly string _key;

    public PowerToolsPermissionEntity(string key) => _key = key;

    public string GetPermissionKey() => _key;

    public IEnumerable<IPermissionEntity> GetPermissionParents() => Enumerable.Empty<IPermissionEntity>();
}

/// <summary>Resolves stored permission keys back to entities — auto-discovered by DW's
/// AddInManager (see PermissionEntityLookupManager).</summary>
public sealed class PowerToolsPermissionEntityLookup : IPermissionEntityLookup
{
    public string PermissionName => PowerToolsPermissionEntity.PermissionName;

    public IPermissionEntity? GetPermissionEntityByKey(string key) =>
        key is PowerToolsPermissionEntity.SecurityViewerKey
            ? new PowerToolsPermissionEntity(key)
            : null;
}

/// <summary>
/// Access checks for the PowerTools suite. The viewer is read-only, so Read on the function
/// grant is the only requirement. Checks fail CLOSED — an exception during evaluation
/// denies access.
/// </summary>
public static class PowerToolsAccess
{
    public static bool CanUseSecurityViewer()
    {
        try
        {
            return new PowerToolsPermissionEntity(PowerToolsPermissionEntity.SecurityViewerKey)
                .GetPermission()
                .HasPermission(PermissionLevel.Read);
        }
        catch
        {
            return false;
        }
    }
}
