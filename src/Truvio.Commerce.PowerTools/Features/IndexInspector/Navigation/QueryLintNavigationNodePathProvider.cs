using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Navigation;

public sealed class QueryLintNavigationNodePathProvider : NavigationNodePathProvider<QueryLintModel>
{
    public QueryLintNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(QueryLintModel? model) =>
        PowerToolsNavigationPaths.For<SearchSection>(SearchNodeProvider.QueryLinterNodeId);
}
