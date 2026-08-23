using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.AdminUI.Models;

namespace Truvio.Commerce.PowerTools.AdminUI.Tree;

// Anchors every Search-section screen (and its drilldowns) under the node it belongs to, so
// the tree keeps the right node highlighted while the user drills in.

public sealed class IndexListNavigationNodePathProvider : NavigationNodePathProvider<IndexListModel>
{
    public IndexListNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(IndexListModel? model) =>
        PowerToolsNavigationPaths.For<SearchSection>(SearchNodeProvider.IndexesNodeId);
}

public sealed class IndexDetailNavigationNodePathProvider : NavigationNodePathProvider<IndexDetailModel>
{
    public IndexDetailNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(IndexDetailModel? model) =>
        PowerToolsNavigationPaths.For<SearchSection>(SearchNodeProvider.IndexesNodeId);
}

public sealed class FieldUsageNavigationNodePathProvider : NavigationNodePathProvider<FieldUsageModel>
{
    public FieldUsageNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(FieldUsageModel? model) =>
        PowerToolsNavigationPaths.For<SearchSection>(SearchNodeProvider.FieldUsageNodeId);
}

public sealed class QueryLintNavigationNodePathProvider : NavigationNodePathProvider<QueryLintModel>
{
    public QueryLintNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(QueryLintModel? model) =>
        PowerToolsNavigationPaths.For<SearchSection>(SearchNodeProvider.QueryLinterNodeId);
}

public sealed class IndexPickNavigationNodePathProvider : NavigationNodePathProvider<IndexPickModel>
{
    public IndexPickNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(IndexPickModel? model) =>
        PowerToolsNavigationPaths.For<SearchSection>(SearchNodeProvider.DocumentsNodeId);
}

public sealed class DocumentRowNavigationNodePathProvider : NavigationNodePathProvider<DocumentRowModel>
{
    public DocumentRowNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(DocumentRowModel? model) =>
        PowerToolsNavigationPaths.For<SearchSection>(SearchNodeProvider.DocumentsNodeId);
}

public sealed class DocumentDetailNavigationNodePathProvider : NavigationNodePathProvider<DocumentDetailModel>
{
    public DocumentDetailNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(DocumentDetailModel? model) =>
        PowerToolsNavigationPaths.For<SearchSection>(SearchNodeProvider.DocumentsNodeId);
}
