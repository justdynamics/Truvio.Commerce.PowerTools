namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

public sealed record QuerySpec(
    string Repository,
    string Item,
    string Name,
    string Description,
    string SourceRepository,
    string SourceItem,
    IReadOnlyList<QueryParameterSpec> Parameters,
    IReadOnlyList<QuerySortSpec> SortOrder,
    QueryNodeSpec? Expression)
{
    public string Key => SearchKeys.For(Repository, Item);

    public string SourceKey => SearchKeys.For(SourceRepository, SourceItem);

    public QueryParameterSpec? Parameter(string? name) =>
        string.IsNullOrEmpty(name)
            ? null
            : Parameters.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Every clause in the expression tree, depth-first.</summary>
    public IEnumerable<QueryClauseSpec> Clauses() => Walk(Expression).OfType<QueryClauseSpec>();

    public IEnumerable<QueryNodeSpec> Nodes() => Walk(Expression);

    private static IEnumerable<QueryNodeSpec> Walk(QueryNodeSpec? node)
    {
        if (node is null)
            yield break;

        yield return node;

        if (node is QueryGroupSpec group)
        {
            foreach (var child in group.Children)
            foreach (var descendant in Walk(child))
                yield return descendant;
        }
    }
}
