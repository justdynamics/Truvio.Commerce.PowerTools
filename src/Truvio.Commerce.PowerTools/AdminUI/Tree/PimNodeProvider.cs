using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Icons;
using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Truvio.Commerce.PowerTools.AdminUI.Screens;
using Truvio.Commerce.PowerTools.AdminUI.Security;

namespace Truvio.Commerce.PowerTools.AdminUI.Tree;

/// <summary>Root nodes of the <see cref="PimSection"/>.</summary>
public sealed class PimNodeProvider : NavigationNodeProvider<PimSection>
{
    // Node IDs cannot contain '/' — DW NavigationNodePath splits on it.
    public const string QualityNodeId = "PowerTools_PimQuality";
    public const string CompletenessNodeId = "PowerTools_PimCompleteness";
    public const string GovernanceNodeId = "PowerTools_PimGovernance";

    public override IEnumerable<NavigationNode> GetRootNodes()
    {
        if (!PowerToolsAccess.CanUsePim())
            yield break;

        yield return new NavigationNode
        {
            Id = QualityNodeId,
            Name = "Catalog quality",
            Icon = Icon.Heartbeat,
            Sort = 10,
            HasSubNodes = false,
            NodeAction = NavigateScreenAction.To<PimQualityScreen>().With(new PimQualityQuery())
        };

        yield return new NavigationNode
        {
            Id = CompletenessNodeId,
            Name = "Completeness explorer",
            Icon = Icon.Tag,
            Sort = 20,
            HasSubNodes = false,
            NodeAction = NavigateScreenAction.To<PimCompletenessScreen>().With(new PimCompletenessQuery())
        };

        yield return new NavigationNode
        {
            Id = GovernanceNodeId,
            Name = "Rules & workflows",
            Icon = Icon.Sitemap,
            Sort = 30,
            HasSubNodes = false,
            NodeAction = NavigateScreenAction.To<PimGovernanceScreen>().With(new PimGovernanceQuery())
        };
    }

    public override IEnumerable<NavigationNode> GetSubNodes(NavigationNodePath parentNodePath)
    {
        yield break;
    }
}
