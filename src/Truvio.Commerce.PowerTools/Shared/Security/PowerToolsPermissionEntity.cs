using Dynamicweb.Security.Permissions;

namespace Truvio.Commerce.PowerTools.Shared.Security;

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
