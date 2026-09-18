using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.Settings.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.Settings.Navigation;

/// <summary>Anchors the settings screen under PowerTools ▸ Settings ▸ PowerTools settings.</summary>
public sealed class PowerToolsSettingsNavigationNodePathProvider : NavigationNodePathProvider<PowerToolsSettingsModel>
{
    public PowerToolsSettingsNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(PowerToolsSettingsModel? model) =>
        PowerToolsNavigationPaths.For<SettingsSection>(SettingsNodeProvider.SettingsNodeId);
}
