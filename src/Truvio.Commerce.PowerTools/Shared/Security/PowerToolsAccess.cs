using Dynamicweb.Security.Permissions;

namespace Truvio.Commerce.PowerTools.Shared.Security;

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
