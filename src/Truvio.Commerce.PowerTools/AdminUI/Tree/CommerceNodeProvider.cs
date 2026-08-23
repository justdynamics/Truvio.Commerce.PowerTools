using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Icons;
using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Truvio.Commerce.PowerTools.AdminUI.Screens;
using Truvio.Commerce.PowerTools.AdminUI.Security;

namespace Truvio.Commerce.PowerTools.AdminUI.Tree;

/// <summary>Root nodes of the <see cref="CommerceSection"/>.</summary>
public sealed class CommerceNodeProvider : NavigationNodeProvider<CommerceSection>
{
    public const string PriceExplainerNodeId = "PowerTools_PriceExplainer";

    public override IEnumerable<NavigationNode> GetRootNodes()
    {
        if (!PowerToolsAccess.CanUsePriceExplainer())
            yield break;

        yield return new NavigationNode
        {
            Id = PriceExplainerNodeId,
            Name = "Price Explainer",
            Icon = Icon.Tag,
            Sort = 10,
            HasSubNodes = false,
            NodeAction = NavigateScreenAction.To<ExplainerAccountListScreen>()
                .With(new ExplainerAccountListQuery())
        };
    }

    public override IEnumerable<NavigationNode> GetSubNodes(NavigationNodePath parentNodePath)
    {
        yield break;
    }
}
