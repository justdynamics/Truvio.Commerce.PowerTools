using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.QueryTester.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.QueryTester.Navigation;

public sealed class QueryWhyNavigationNodePathProvider : NavigationNodePathProvider<QueryWhyModel>
{
    public QueryWhyNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(QueryWhyModel? model) =>
        PowerToolsNavigationPaths.For<SearchSection>(SearchNodeProvider.QueryTesterNodeId);
}
