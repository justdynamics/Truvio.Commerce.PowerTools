namespace Truvio.Commerce.PowerTools.Core.Search;

/// <summary>
/// One resolved view over every repository: indexes by key, and the queries / facet groups
/// that point at them. Rules and the field-usage map are pure functions of this catalog.
/// </summary>
public sealed class SearchCatalog
{
    private readonly Dictionary<string, IndexSpec> _indexesByKey;
    private readonly Dictionary<string, QuerySpec> _queriesByKey;

    public SearchCatalog(IReadOnlyList<RepositorySpec> repositories)
    {
        Repositories = repositories;
        Indexes = repositories.SelectMany(r => r.Indexes).ToList();
        Queries = repositories.SelectMany(r => r.Queries).ToList();
        FacetGroups = repositories.SelectMany(r => r.FacetGroups).ToList();

        _indexesByKey = new Dictionary<string, IndexSpec>(StringComparer.OrdinalIgnoreCase);
        foreach (var index in Indexes)
            _indexesByKey[index.Key] = index;

        _queriesByKey = new Dictionary<string, QuerySpec>(StringComparer.OrdinalIgnoreCase);
        foreach (var query in Queries)
            _queriesByKey[query.Key] = query;
    }

    public static SearchCatalog From(ISearchSource source) => new(source.GetRepositories());

    public IReadOnlyList<RepositorySpec> Repositories { get; }

    public IReadOnlyList<IndexSpec> Indexes { get; }

    public IReadOnlyList<QuerySpec> Queries { get; }

    public IReadOnlyList<FacetGroupSpec> FacetGroups { get; }

    public IndexSpec? Index(string? key) =>
        !string.IsNullOrEmpty(key) && _indexesByKey.TryGetValue(key, out var index) ? index : null;

    public IndexSpec? Index(string? repository, string? item) => Index(SearchKeys.For(repository, item));

    public QuerySpec? Query(string? key) =>
        !string.IsNullOrEmpty(key) && _queriesByKey.TryGetValue(key, out var query) ? query : null;

    public QuerySpec? Query(string? repository, string? item) => Query(SearchKeys.For(repository, item));

    /// <summary>The index a query reads from, or null when its source does not resolve.</summary>
    public IndexSpec? IndexFor(QuerySpec query) => Index(query.SourceKey);

    /// <summary>Every query whose source is this index.</summary>
    public IReadOnlyList<QuerySpec> QueriesFor(IndexSpec index) =>
        Queries.Where(q => SearchKeys.Same(q.SourceKey, index.Key)).ToList();

    /// <summary>Every facet group whose source query reads from this index.</summary>
    public IReadOnlyList<FacetGroupSpec> FacetGroupsFor(IndexSpec index) =>
        FacetGroups.Where(g =>
        {
            var query = Query(g.SourceKey);
            return query is not null && SearchKeys.Same(query.SourceKey, index.Key);
        }).ToList();

    public IReadOnlyList<FacetGroupSpec> FacetGroupsForQuery(QuerySpec query) =>
        FacetGroups.Where(g => SearchKeys.Same(g.SourceKey, query.Key)).ToList();

    /// <summary>The index a facet group ultimately faces, resolved through its source query.</summary>
    public IndexSpec? IndexFor(FacetGroupSpec group)
    {
        var query = Query(group.SourceKey);
        return query is null ? null : IndexFor(query);
    }

    public string Describe(QuerySpec query) => $"{query.Name} ({query.Repository})";

    public string Describe(IndexSpec index) => $"{index.Name} ({index.Repository})";

    public string Describe(FacetGroupSpec group) => $"{group.Name} ({group.Repository})";
}
