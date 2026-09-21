using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.QueryTester.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.QueryTester.Navigation;

// Anchors the Query tester's three screens under PowerTools ▸ Search ▸ Query tester, so the
// tree keeps that node highlighted while the user drills from the picker into the report.

public sealed class QueryPickNavigationNodePathProvider : NavigationNodePathProvider<QueryPickModel>
{
    public QueryPickNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(QueryPickModel? model) =>
        PowerToolsNavigationPaths.For<SearchSection>(SearchNodeProvider.QueryTesterNodeId);
}
