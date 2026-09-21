using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Icons;
using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.Settings.Queries;
using Truvio.Commerce.PowerTools.Features.Settings.Screens;
using Truvio.Commerce.PowerTools.Shared.Security;

namespace Truvio.Commerce.PowerTools.Shared.Navigation;

/// <summary>Root nodes of the <see cref="SettingsSection"/>.</summary>
public sealed class SettingsNodeProvider : NavigationNodeProvider<SettingsSection>
{
    // Node IDs cannot contain '/' — DW NavigationNodePath splits on it.
    public const string SettingsNodeId = "PowerTools_Settings";

    public override IEnumerable<NavigationNode> GetRootNodes()
    {
        if (!PowerToolsAccess.CanViewSettings())
            yield break;

        yield return new NavigationNode
        {
            Id = SettingsNodeId,
            Name = "PowerTools settings",
            Icon = Icon.Cog,
            Sort = 10,
            HasSubNodes = false,
            NodeAction = NavigateScreenAction.To<PowerToolsSettingsScreen>().With(new PowerToolsSettingsQuery())
        };
    }

    public override IEnumerable<NavigationNode> GetSubNodes(NavigationNodePath parentNodePath)
    {
        yield break;
    }
}
