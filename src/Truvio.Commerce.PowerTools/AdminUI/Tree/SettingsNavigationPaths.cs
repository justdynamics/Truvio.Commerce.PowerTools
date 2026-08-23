using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.AdminUI.Models;

namespace Truvio.Commerce.PowerTools.AdminUI.Tree;

/// <summary>Anchors the settings screen under PowerTools ▸ Settings ▸ PowerTools settings.</summary>
public sealed class PowerToolsSettingsNavigationNodePathProvider : NavigationNodePathProvider<PowerToolsSettingsModel>
{
    public PowerToolsSettingsNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(PowerToolsSettingsModel? model) =>
        PowerToolsNavigationPaths.For<SettingsSection>(SettingsNodeProvider.SettingsNodeId);
}
