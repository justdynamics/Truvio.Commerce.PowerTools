using Dynamicweb.CoreUI.Data;
using Dynamicweb.Security.Permissions;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.AdminUI.Security;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// Loads the suite settings. Constructing the model is the load: <c>SettingsViewModelBase</c>
/// pulls every <c>[Settings]</c> property out of GlobalSettings in its constructor.
/// <para>
/// <c>PermissionLevelCurrentUser</c> is what drives the Save button: CoreUI's
/// <c>EditScreenBase.AddSaveButtons</c>/<c>GetSubmitAction</c> only render Save when the model
/// reports Edit. Without Edit on the settings grant the screen stays a read-only view.
/// </para>
/// </summary>
public sealed class PowerToolsSettingsQuery : DataQueryModelBase<PowerToolsSettingsModel>
{
    public override PowerToolsSettingsModel? GetModel() => new()
    {
        PermissionLevelCurrentUser = PowerToolsAccess.CanEditSettings()
            ? PermissionLevel.All
            : PermissionLevel.Read
    };
}
