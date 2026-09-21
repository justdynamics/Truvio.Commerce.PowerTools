using Dynamicweb.Security.Permissions;

namespace Truvio.Commerce.PowerTools.Shared.Security;

/// <summary>Resolves stored permission keys back to entities — auto-discovered by DW's
/// AddInManager (see PermissionEntityLookupManager).</summary>
public sealed class PowerToolsPermissionEntityLookup : IPermissionEntityLookup
{
    public string PermissionName => PowerToolsPermissionEntity.PermissionName;

    public IPermissionEntity? GetPermissionEntityByKey(string key) =>
        PowerToolsPermissionEntity.AllKeys.Contains(key)
            ? new PowerToolsPermissionEntity(key)
            : null;
}
