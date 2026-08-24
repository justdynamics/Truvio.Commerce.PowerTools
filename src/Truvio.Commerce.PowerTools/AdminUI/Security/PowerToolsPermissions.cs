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

    public const string OperationsKey = "truvio-powertools-operations";

    public const string SearchInspectorKey = "truvio-powertools-search";

    public const string PimKey = "truvio-powertools-pim";

    /// <summary>
    /// Own grant, deliberately not folded into the Security Viewer key: the backend rights
    /// report exposes who-can-do-what across the whole admin, which is more sensitive than
    /// content visibility.
    /// </summary>
    public const string BackendRightsKey = "truvio-powertools-backend-rights";

    /// <summary>Suite settings: Read to look, Edit to change. Everything else is read-only.</summary>
    public const string SettingsKey = "truvio-powertools-settings";

    /// <summary>Every function grant the suite exposes, in display order.</summary>
    public static readonly IReadOnlyList<string> AllKeys = [SecurityViewerKey, BackendRightsKey, PriceExplainerKey, PimKey, OperationsKey, SearchInspectorKey, SettingsKey];

    /// <summary>The tool grants — every key except the settings grant.</summary>
    public static readonly IReadOnlyList<string> ToolKeys = [SecurityViewerKey, BackendRightsKey, PriceExplainerKey, PimKey, OperationsKey, SearchInspectorKey];

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

    public static bool CanUseOperations() => HasRead(PowerToolsPermissionEntity.OperationsKey);

    public static bool CanUseSearchInspector() => HasRead(PowerToolsPermissionEntity.SearchInspectorKey);

    public static bool CanUsePim() => HasRead(PowerToolsPermissionEntity.PimKey);

    public static bool CanUseBackendRights() => HasRead(PowerToolsPermissionEntity.BackendRightsKey);

    /// <summary>Looking at the settings needs nothing beyond access to any one tool.</summary>
    public static bool CanViewSettings() =>
        PowerToolsPermissionEntity.AllKeys.Any(HasRead);

    /// <summary>Changing them is a write, so it needs Edit on the settings grant specifically.</summary>
    public static bool CanEditSettings() =>
        HasLevel(PowerToolsPermissionEntity.SettingsKey, PermissionLevel.Edit);

    private static bool HasRead(string key) => HasLevel(key, PermissionLevel.Read);

    private static bool HasLevel(string key, PermissionLevel level)
    {
        try
        {
            return new PowerToolsPermissionEntity(key)
                .GetPermission()
                .HasPermission(level);
        }
        catch
        {
            return false;
        }
    }
}
