using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Navigation;

public sealed class PriceExplainNavigationNodePathProvider : NavigationNodePathProvider<PriceExplainModel>
{
    public PriceExplainNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(PriceExplainModel? model) =>
        PowerToolsNavigationPaths.For<CommerceSection>(CommerceNodeProvider.PriceExplainerNodeId);
}
