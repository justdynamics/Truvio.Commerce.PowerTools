using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Icons;
using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Truvio.Commerce.PowerTools.AdminUI.Screens;
using Truvio.Commerce.PowerTools.AdminUI.Security;

namespace Truvio.Commerce.PowerTools.AdminUI.Tree;

/// <summary>
/// Root nodes of the <see cref="SecuritySection"/>: the security tools sit directly in
/// their section, no wrapper node.
/// </summary>
public sealed class SecurityNodeProvider : NavigationNodeProvider<SecuritySection>
{
    // Node IDs cannot contain '/' — DW NavigationNodePath splits on it.
    public const string SecurityViewerNodeId = "PowerTools_SecurityViewer";
    public const string WarningsNodeId = "PowerTools_Warnings";
    public const string BackendRightsNodeId = "PowerTools_BackendRights";

    public override IEnumerable<NavigationNode> GetRootNodes()
    {
        // Each tool carries its own grant: the backend rights report exposes who-can-do-what across
        // the whole admin, so it is not implied by access to the content viewer.
        if (PowerToolsAccess.CanUseBackendRights())
        {
            yield return new NavigationNode
            {
                Id = BackendRightsNodeId,
                Name = "Backend Rights Viewer",
                Icon = Icon.UserCircle,
                Sort = 30,
                HasSubNodes = false,
                NodeAction = NavigateScreenAction.To<BackendRightsListScreen>()
                    .With(new BackendRightsListQuery())
            };
        }

        if (!PowerToolsAccess.CanUseSecurityViewer())
            yield break;

        yield return new NavigationNode
        {
            Id = SecurityViewerNodeId,
            Name = "Content Access Viewer",
            Icon = Icon.Shield,
            Sort = 10,
            HasSubNodes = false,
            NodeAction = NavigateScreenAction.To<AccessOverviewScreen>()
                .With(new AccessOverviewQuery())
        };

        yield return new NavigationNode
        {
            Id = WarningsNodeId,
            Name = "Content Access Warnings",
            Icon = Icon.ExclamationTriangle,
            Sort = 20,
            HasSubNodes = false,
            NodeAction = NavigateScreenAction.To<WarningListScreen>()
                .With(new FindingListQuery())
        };
    }

    public override IEnumerable<NavigationNode> GetSubNodes(NavigationNodePath parentNodePath)
    {
        yield break;
    }
}
