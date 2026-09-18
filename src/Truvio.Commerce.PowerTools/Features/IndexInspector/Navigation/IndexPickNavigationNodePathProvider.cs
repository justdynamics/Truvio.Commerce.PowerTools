using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Navigation;

public sealed class IndexPickNavigationNodePathProvider : NavigationNodePathProvider<IndexPickModel>
{
    public IndexPickNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(IndexPickModel? model) =>
        PowerToolsNavigationPaths.For<SearchSection>(SearchNodeProvider.DocumentsNodeId);
}
