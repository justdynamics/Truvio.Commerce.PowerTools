using Dynamicweb.CoreUI.Data;
using Dynamicweb.Extensibility.Settings;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.AdminUI.Security;

namespace Truvio.Commerce.PowerTools.AdminUI.Commands;

/// <summary>
/// The one write in the whole suite: persists the PowerTools settings into GlobalSettings.
/// <para>
/// <c>SettingsService.Persist</c> walks the model's <c>[Settings]</c> properties and calls
/// <c>SystemConfiguration.Instance.SetValue</c> for each; SetValue itself persists the
/// provider (verified: <c>Dynamicweb.Configuration.ConfigurationManager.SetValue</c> calls
/// <c>provider.Persist()</c>), so no separate Save step exists.
/// </para>
/// <para>
/// The screen already hides Save without Edit on the settings grant, but a command must check
/// for itself — that is DW's own rule on <c>DataViewModelBase.PermissionLevelCurrentUser</c>.
/// </para>
/// </summary>
public sealed class PowerToolsSettingsSaveCommand : CommandBase<PowerToolsSettingsModel>
{
    public override CommandResult Handle()
    {
        var model = GetModel();

        if (!PowerToolsAccess.CanEditSettings())
        {
            return new CommandResult
            {
                Model = model,
                Status = CommandResult.ResultType.NotAllowed,
                Message = "Changing PowerTools settings requires Edit on the 'Truvio PowerTools' settings permission."
            };
        }

        SettingsService.Persist(model);

        return new CommandResult
        {
            Model = model,
            Status = CommandResult.ResultType.Ok
        };
    }
}
