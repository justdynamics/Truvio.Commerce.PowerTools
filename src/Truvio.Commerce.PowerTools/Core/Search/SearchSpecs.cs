namespace Truvio.Commerce.PowerTools.Core.Search;

/// <summary>
/// Pure, DW-free snapshot records describing what lives in the repositories: indexes with
/// their schema, instances and builders; queries with their parameters, expression tree and
/// sort order; facet groups. Everything the Index &amp; Query Inspector reasons about is
/// expressed over these records so the rules stay unit-testable.
/// </summary>
public sealed record IndexFieldSpec(
    string SystemName,
    string Name,
    string TypeName,
    string Analyzer,
    string Boost,
    bool Stored,
    bool Indexed,
    bool Analyzed,
    string Group,
    string Source,
    string Kind)
{
    /// <summary>"System.String[]" -&gt; "String[]" for display.</summary>
    public string ShortTypeName => ShortenType(TypeName);

    internal static string ShortenType(string? typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return string.Empty;

        var name = typeName.Split(',')[0].Trim();
        var array = name.EndsWith("[]", StringComparison.Ordinal);
        if (array)
            name = name[..^2];

        var dot = name.LastIndexOf('.');
        if (dot >= 0 && dot < name.Length - 1)
            name = name[(dot + 1)..];

        return array ? name + "[]" : name;
    }
}

public sealed record IndexInstanceSpec(
    string Name,
    string ProviderType,
    bool IsOnline,
    bool IsAvailable,
    string State,
    DateTime? LastBuild,
    TimeSpan? Duration);

public sealed record IndexBuildSpec(
    string Name,
    string Action,
    string BuilderType,
    IReadOnlyList<KeyValuePair<string, string>> Settings);

/// <summary>Health of an index, derived from its instances' last build status.</summary>
public enum IndexHealth
{
    Ok,
    Stale,
    NeverBuilt,
    Failed
}

public sealed record IndexSpec(
    string Repository,
    string Item,
    string Name,
    string Balancer,
    string BuilderType,
    IReadOnlyList<IndexFieldSpec> Fields,
    IReadOnlyList<IndexInstanceSpec> Instances,
    IReadOnlyList<IndexBuildSpec> Builds,
    IndexHealth Health,
    string HealthDetail,
    DateTime? LastBuild,
    string OnlineInstance)
{
    /// <summary>Repository-qualified item name; the identity a query's source points at.</summary>
    public string Key => SearchKeys.For(Repository, Item);

    public IndexFieldSpec? Field(string? systemName) =>
        string.IsNullOrEmpty(systemName)
            ? null
            : Fields.FirstOrDefault(f => string.Equals(f.SystemName, systemName, StringComparison.Ordinal));
}

public sealed record QueryParameterSpec(string Name, string TypeName, string DefaultValue)
{
    /// <summary>
    /// A parameter only ever reaches the index provider when it has a non-empty default (or
    /// an explicit runtime value) — see <c>LuceneQueryProvider.HandleParameters</c>.
    /// </summary>
    public bool HasDefault => !string.IsNullOrEmpty(DefaultValue);
}

public sealed record QuerySortSpec(string Field, string Direction);

/// <summary>What sits on the right-hand side of a binary clause.</summary>
public enum ClauseValueKind
{
    Constant,
    Term,
    Parameter,
    Macro,
    Code,
    Unknown
}

public abstract record QueryNodeSpec(string Path);

public sealed record QueryGroupSpec(
    string Path,
    string Operator,
    bool Negate,
    IReadOnlyList<QueryNodeSpec> Children) : QueryNodeSpec(Path)
{
    /// <summary>An And-group ANDs its children (Lucene Occur.MUST); anything else ORs them.</summary>
    public bool IsAnd => string.Equals(Operator, "And", StringComparison.OrdinalIgnoreCase);
}

public sealed record QueryClauseSpec(
    string Path,
    string FieldName,
    string Operator,
    ClauseValueKind ValueKind,
    string ParameterName,
    string Value,
    bool Disabled) : QueryNodeSpec(Path)
{
    public override string ToString() =>
        $"{FieldName} {Operator} {(ValueKind == ClauseValueKind.Parameter ? "@" + ParameterName : Value)}";
}

public sealed record QueryFullTextSpec(
    string Path,
    IReadOnlyList<string> Fields,
    string SearchText) : QueryNodeSpec(Path);

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

public sealed record FacetSpec(
    string Name,
    string Field,
    string QueryParameter,
    string TypeName,
    string RenderType);

public sealed record FacetGroupSpec(
    string Repository,
    string Item,
    string Name,
    string SourceRepository,
    string SourceItem,
    IReadOnlyList<FacetSpec> Facets)
{
    public string Key => SearchKeys.For(Repository, Item);

    public string SourceKey => SearchKeys.For(SourceRepository, SourceItem);
}

public sealed record RepositorySpec(
    string Name,
    string Description,
    IReadOnlyList<IndexSpec> Indexes,
    IReadOnlyList<QuerySpec> Queries,
    IReadOnlyList<FacetGroupSpec> FacetGroups);

/// <summary>Repository item keys are compared case-insensitively, like the file system.</summary>
public static class SearchKeys
{
    public static string For(string? repository, string? item) =>
        string.IsNullOrEmpty(repository) || string.IsNullOrEmpty(item)
            ? string.Empty
            : $"{repository}/{item}";

    public static bool Same(string? a, string? b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Reads the repository definitions. The DW adapter is the only implementation.</summary>
public interface ISearchSource
{
    IReadOnlyList<RepositorySpec> GetRepositories();
}
