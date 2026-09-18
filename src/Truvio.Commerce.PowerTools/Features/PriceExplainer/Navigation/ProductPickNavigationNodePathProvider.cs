using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Navigation;

public sealed class ProductPickNavigationNodePathProvider : NavigationNodePathProvider<ProductPickModel>
{
    public ProductPickNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(ProductPickModel? model) =>
        PowerToolsNavigationPaths.For<CommerceSection>(CommerceNodeProvider.PriceExplainerNodeId);
}
