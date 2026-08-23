using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.AdminUI.Models;

namespace Truvio.Commerce.PowerTools.AdminUI.Tree;

// Anchors the Query tester's three screens under PowerTools ▸ Search ▸ Query tester, so the
// tree keeps that node highlighted while the user drills from the picker into the report.

public sealed class QueryPickNavigationNodePathProvider : NavigationNodePathProvider<QueryPickModel>
{
    public QueryPickNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(QueryPickModel? model) =>
        PowerToolsNavigationPaths.For<SearchSection>(SearchNodeProvider.QueryTesterNodeId);
}

public sealed class QueryParameterNavigationNodePathProvider : NavigationNodePathProvider<QueryParameterModel>
{
    public QueryParameterNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(QueryParameterModel? model) =>
        PowerToolsNavigationPaths.For<SearchSection>(SearchNodeProvider.QueryTesterNodeId);
}

public sealed class QueryTestNavigationNodePathProvider : NavigationNodePathProvider<QueryTestModel>
{
    public QueryTestNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(QueryTestModel? model) =>
        PowerToolsNavigationPaths.For<SearchSection>(SearchNodeProvider.QueryTesterNodeId);
}
