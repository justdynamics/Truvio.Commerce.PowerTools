using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.QueryTester.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.QueryTester.Navigation;

public sealed class QueryValuesNavigationNodePathProvider : NavigationNodePathProvider<QueryValuesModel>
{
    public QueryValuesNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(QueryValuesModel? model) =>
        PowerToolsNavigationPaths.For<SearchSection>(SearchNodeProvider.QueryTesterNodeId);
}
