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

    public const string PriceExplainerKey = "truvio-powertools-price-explainer";

    public const string SearchInspectorKey = "truvio-powertools-search";

    /// <summary>Every function grant the suite exposes, in display order.</summary>
    public static readonly IReadOnlyList<string> AllKeys = [SecurityViewerKey, PriceExplainerKey, SearchInspectorKey];

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
        PowerToolsPermissionEntity.AllKeys.Contains(key)
            ? new PowerToolsPermissionEntity(key)
            : null;
}

/// <summary>
/// Access checks for the PowerTools suite. Every tool is read-only, so Read on its function
/// grant is the only requirement. Checks fail CLOSED — an exception during evaluation
/// denies access.
/// </summary>
public static class PowerToolsAccess
{
    public static bool CanUseSecurityViewer() => HasRead(PowerToolsPermissionEntity.SecurityViewerKey);

    public static bool CanUsePriceExplainer() => HasRead(PowerToolsPermissionEntity.PriceExplainerKey);

    public static bool CanUseSearchInspector() => HasRead(PowerToolsPermissionEntity.SearchInspectorKey);

    private static bool HasRead(string key)
    {
        try
        {
            return new PowerToolsPermissionEntity(key)
                .GetPermission()
                .HasPermission(PermissionLevel.Read);
        }
        catch
        {
            return false;
        }
    }
}
