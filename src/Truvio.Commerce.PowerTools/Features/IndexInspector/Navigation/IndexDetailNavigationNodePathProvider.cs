using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Navigation;

public sealed class IndexDetailNavigationNodePathProvider : NavigationNodePathProvider<IndexDetailModel>
{
    public IndexDetailNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(IndexDetailModel? model) =>
        PowerToolsNavigationPaths.For<SearchSection>(SearchNodeProvider.IndexesNodeId);
}
