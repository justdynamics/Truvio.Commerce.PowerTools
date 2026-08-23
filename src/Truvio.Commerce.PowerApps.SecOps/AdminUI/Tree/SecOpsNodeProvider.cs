using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Icons;
using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerApps.SecOps.AdminUI.Queries;
using Truvio.Commerce.PowerApps.SecOps.AdminUI.Screens;
using Truvio.Commerce.PowerApps.SecOps.AdminUI.Security;

namespace Truvio.Commerce.PowerApps.SecOps.AdminUI.Tree;

/// <summary>
/// Root nodes of the <see cref="ToolsSection"/>: the SecOps tools sit directly in their own
/// section, no wrapper node.
/// </summary>
public sealed class SecOpsNodeProvider : NavigationNodeProvider<ToolsSection>
{
    // Node IDs cannot contain '/' — DW NavigationNodePath splits on it.
    public const string SecurityViewerNodeId = "SecOps_SecurityViewer";
    public const string WarningsNodeId = "SecOps_Warnings";

    public override IEnumerable<NavigationNode> GetRootNodes()
    {
        if (!SecOpsAccess.CanUseSecurityViewer())
            yield break;

        yield return new NavigationNode
        {
            Id = SecurityViewerNodeId,
            Name = "Security Viewer",
            Icon = Icon.Shield,
            Sort = 10,
            HasSubNodes = false,
            NodeAction = NavigateScreenAction.To<AccountListScreen>()
                .With(new AccountListQuery())
        };

        yield return new NavigationNode
        {
            Id = WarningsNodeId,
            Name = "Warnings",
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
