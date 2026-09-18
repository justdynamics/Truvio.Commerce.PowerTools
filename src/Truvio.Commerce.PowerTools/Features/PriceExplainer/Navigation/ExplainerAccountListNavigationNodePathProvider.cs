using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Navigation;

// ---- Commerce section --------------------------------------------------------------------

public sealed class ExplainerAccountListNavigationNodePathProvider : NavigationNodePathProvider<ExplainerAccountModel>
{
    public ExplainerAccountListNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(ExplainerAccountModel? model) =>
        PowerToolsNavigationPaths.For<CommerceSection>(CommerceNodeProvider.PriceExplainerNodeId);
}
