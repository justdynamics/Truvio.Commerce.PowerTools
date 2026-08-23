using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Icons;
using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Truvio.Commerce.PowerTools.AdminUI.Screens;
using Truvio.Commerce.PowerTools.AdminUI.Security;

namespace Truvio.Commerce.PowerTools.AdminUI.Tree;

/// <summary>Root nodes of the <see cref="SearchSection"/>.</summary>
public sealed class SearchNodeProvider : NavigationNodeProvider<SearchSection>
{
    // Node IDs cannot contain '/' — DW NavigationNodePath splits on it.
    public const string IndexesNodeId = "PowerTools_Indexes";
    public const string FieldUsageNodeId = "PowerTools_FieldUsage";
    public const string QueryLinterNodeId = "PowerTools_QueryLinter";
    public const string DocumentsNodeId = "PowerTools_Documents";

    public override IEnumerable<NavigationNode> GetRootNodes()
    {
        if (!PowerToolsAccess.CanUseSearchInspector())
            yield break;

        yield return new NavigationNode
        {
            Id = IndexesNodeId,
            Name = "Repositories & indexes",
            Icon = Icon.Database,
            Sort = 10,
            HasSubNodes = false,
            NodeAction = NavigateScreenAction.To<IndexListScreen>().With(new IndexListQuery())
        };

        yield return new NavigationNode
        {
            Id = FieldUsageNodeId,
            Name = "Field where-used",
            Icon = Icon.Sitemap,
            Sort = 20,
            HasSubNodes = false,
            NodeAction = NavigateScreenAction.To<FieldUsageScreen>().With(new FieldUsageQuery())
        };

        yield return new NavigationNode
        {
            Id = QueryLinterNodeId,
            Name = "Query linter",
            Icon = Icon.Bug,
            Sort = 30,
            HasSubNodes = false,
            NodeAction = NavigateScreenAction.To<QueryLintScreen>().With(new QueryLintQuery())
        };

        yield return new NavigationNode
        {
            Id = DocumentsNodeId,
            Name = "Document browser",
            Icon = Icon.Table,
            Sort = 40,
            HasSubNodes = false,
            NodeAction = NavigateScreenAction.To<IndexPickScreen>().With(new IndexPickQuery())
        };
    }

    public override IEnumerable<NavigationNode> GetSubNodes(NavigationNodePath parentNodePath)
    {
        yield break;
    }
}
