using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.QueryTester.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.QueryTester.Navigation;

public sealed class QueryTestNavigationNodePathProvider : NavigationNodePathProvider<QueryTestModel>
{
    public QueryTestNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(QueryTestModel? model) =>
        PowerToolsNavigationPaths.For<SearchSection>(SearchNodeProvider.QueryTesterNodeId);
}
