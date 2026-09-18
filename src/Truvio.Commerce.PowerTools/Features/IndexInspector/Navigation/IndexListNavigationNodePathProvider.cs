using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Navigation;

// Anchors every Search-section screen (and its drilldowns) under the node it belongs to, so
// the tree keeps the right node highlighted while the user drills in.

public sealed class IndexListNavigationNodePathProvider : NavigationNodePathProvider<IndexListModel>
{
    public IndexListNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(IndexListModel? model) =>
        PowerToolsNavigationPaths.For<SearchSection>(SearchNodeProvider.IndexesNodeId);
}
