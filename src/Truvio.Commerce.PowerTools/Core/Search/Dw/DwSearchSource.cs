using Dynamicweb.Extensibility;
using Dynamicweb.Indexing;
using Dynamicweb.Indexing.Querying;
using Dynamicweb.Indexing.Querying.Expressions;
using Dynamicweb.Indexing.Repositories;
using Dynamicweb.Indexing.Schemas;
using DwExpression = Dynamicweb.Indexing.Querying.Expressions.Expression;

namespace Truvio.Commerce.PowerTools.Core.Search.Dw;

/// <summary>
/// Reads the repository definitions through DW's public indexing API and maps them onto the
/// pure specs the inspector reasons about.
/// <para>
/// Repositories are folders under <c>/Files/System/Repositories</c>; the platform exposes them
/// through <see cref="IRepositoryService"/> (repository names and their items),
/// <see cref="IIndexService"/> (<c>.index</c> files) and <see cref="IQueryService"/>
/// (<c>.query</c> and <c>.facets</c> files). The item TypeName strings are the ones the
/// built-in repository providers emit: "Index", "Query", "Facets".
/// </para>
/// </summary>
public sealed class DwSearchSource : ISearchSource
{
    /// <summary>The platform's own staleness threshold (IndexHelper flags a warning past 24h).</summary>
    private static readonly TimeSpan StaleAfter = TimeSpan.FromHours(24);

    private const string IndexItemType = "Index";
    private const string QueryItemType = "Query";
    private const string FacetsItemType = "Facets";

    private readonly IRepositoryService _repositories;
    private readonly IIndexService _indexes;
    private readonly IQueryService _queries;

    public DwSearchSource()
        : this(
            ServiceLocator.Current.GetInstance<IRepositoryService>(),
            ServiceLocator.Current.GetInstance<IIndexService>(),
            ServiceLocator.Current.GetInstance<IQueryService>())
    {
    }

    public DwSearchSource(IRepositoryService repositories, IIndexService indexes, IQueryService queries)
    {
        _repositories = repositories;
        _indexes = indexes;
        _queries = queries;
    }

    public IReadOnlyList<RepositorySpec> GetRepositories()
    {
        var result = new List<RepositorySpec>();

        foreach (var name in SafeRepositoryNames().OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
        {
            var items = Safe(() => _repositories.GetRepositoryItems(name)?.ToList(), []) ?? [];

            var indexes = new List<IndexSpec>();
            var queries = new List<QuerySpec>();
            var facetGroups = new List<FacetGroupSpec>();

            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item?.Name))
                    continue;

                if (IndexItemType.Equals(item.TypeName, StringComparison.OrdinalIgnoreCase))
                {
                    var index = Safe(() => MapIndex(name, item.Name), null);
                    if (index is not null)
                        indexes.Add(index);
                }
                else if (QueryItemType.Equals(item.TypeName, StringComparison.OrdinalIgnoreCase))
                {
                    var query = Safe(() => MapQuery(name, item.Name), null);
                    if (query is not null)
                        queries.Add(query);
                }
                else if (FacetsItemType.Equals(item.TypeName, StringComparison.OrdinalIgnoreCase))
                {
                    var facets = Safe(() => MapFacets(name, item.Name), null);
                    if (facets is not null)
                        facetGroups.Add(facets);
                }
            }

            var description = Safe(() => _repositories.GetRepositoryInfo(name)?.Description, null) ?? string.Empty;

            result.Add(new RepositorySpec(
                name,
                description,
                indexes.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ToList(),
                queries.OrderBy(q => q.Name, StringComparer.OrdinalIgnoreCase).ToList(),
                facetGroups.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList()));
        }

        return result;
    }

    private IEnumerable<string> SafeRepositoryNames() =>
        Safe(() => _repositories.GetRepositories()?.ToList(), []) ?? [];

    // ---- indexes ---------------------------------------------------------------------

    private IndexSpec? MapIndex(string repository, string item)
    {
        var index = _indexes.LoadIndex(repository, item);
        if (index is null)
            return null;

        var onlineInstance = Safe(() => index.GetInstance()?.Name, null) ?? string.Empty;

        var instances = new List<IndexInstanceSpec>();
        DateTime? lastBuild = null;
        var anyFailed = false;
        var missingHistory = 0;

        foreach (var provider in index.Instances?.Values ?? [])
        {
            if (provider is null)
                continue;

            var instanceName = provider.Name ?? string.Empty;
            var status = Safe(() => IndexHelper.GetInstanceLatestStatus(repository, item, instanceName), null);
            if (status is null)
                missingHistory++;
            else
            {
                if (status.State == Dynamicweb.Diagnostics.Tracking.TrackingState.Failed)
                    anyFailed = true;
                if (!lastBuild.HasValue || status.EndTime > lastBuild.Value)
                    lastBuild = status.EndTime;
            }

            instances.Add(new IndexInstanceSpec(
                instanceName,
                ShortType(provider.Type),
                string.Equals(instanceName, onlineInstance, StringComparison.OrdinalIgnoreCase),
                Safe(() => provider.IsAvailable, false),
                status is null ? "Never built" : status.State.ToString(),
                status?.EndTime,
                status is null ? null : status.EndTime - status.StartTime));
        }

        var (health, healthDetail) = Health(instances.Count, missingHistory, anyFailed, lastBuild);

        var builds = new List<IndexBuildSpec>();
        var builderType = string.Empty;
        foreach (var build in index.Builds?.Values ?? [])
        {
            if (build is null)
                continue;
            builderType = ShortType(build.Type);
            builds.Add(new IndexBuildSpec(
                build.Name ?? string.Empty,
                build.Action ?? string.Empty,
                builderType,
                (build.Settings ?? new Dictionary<string, string>())
                    .OrderBy(s => s.Key, StringComparer.OrdinalIgnoreCase)
                    .ToList()));
        }

        var fields = (index.Schema?.Fields ?? [])
            .Where(f => f is not null)
            .Select(MapField)
            .OrderBy(f => f.SystemName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new IndexSpec(
            repository,
            item,
            Display(index.Name, item),
            ShortType(Safe(() => index.Balancer?.GetType().Name, null) ?? string.Empty),
            builderType,
            fields,
            instances,
            builds,
            health,
            healthDetail,
            lastBuild,
            onlineInstance);
    }

    private static (IndexHealth, string) Health(int instanceCount, int missingHistory, bool anyFailed, DateTime? lastBuild)
    {
        if (anyFailed)
            return (IndexHealth.Failed, "At least one instance failed on its last build.");

        if (!lastBuild.HasValue)
            return (IndexHealth.NeverBuilt, "No instance of this index has a completed build.");

        if (missingHistory > 0)
            return (IndexHealth.Stale,
                $"{missingHistory} of {instanceCount} instance(s) have never been built.");

        return DateTime.Now - lastBuild.Value > StaleAfter
            ? (IndexHealth.Stale, "The newest instance build is older than 24 hours.")
            : (IndexHealth.Ok, "All instances built successfully.");
    }

    private static IndexFieldSpec MapField(FieldDefinitionBase field) =>
        new(
            field.SystemName ?? string.Empty,
            field.Name ?? string.Empty,
            field.TypeName ?? string.Empty,
            ShortType(field.AnalyzerTypeName),
            field.Boost ?? string.Empty,
            field.Stored,
            field.Indexed,
            field.Analyzed,
            field.Group ?? string.Empty,
            field is FieldDefinition definition ? definition.Source ?? string.Empty : string.Empty,
            field.ClassName ?? string.Empty);

    // ---- queries ---------------------------------------------------------------------

    private QuerySpec? MapQuery(string repository, string item)
    {
        var query = _queries.LoadQuery(repository, item);
        if (query is null)
            return null;

        var parameters = (query.Parameters ?? [])
            .Where(p => p is not null)
            .Select(p => new QueryParameterSpec(
                p.Name ?? string.Empty,
                p.TypeName ?? string.Empty,
                p.DefaultValue ?? string.Empty))
            .ToList();

        var sortOrder = (query.SortOrder ?? [])
            .Where(s => s is not null)
            .Select(s => new QuerySortSpec(s.Field ?? string.Empty, s.SortDirection.ToString()))
            .ToList();

        return new QuerySpec(
            repository,
            item,
            Display(query.Name, item),
            query.Description ?? string.Empty,
            query.Source?.Repository ?? string.Empty,
            query.Source?.Item ?? string.Empty,
            parameters,
            sortOrder,
            MapExpression(query.Expression, "1"));
    }

    internal static QueryNodeSpec? MapExpression(DwExpression? expression, string path)
    {
        switch (expression)
        {
            case null:
                return null;

            case GroupExpression group:
            {
                var children = new List<QueryNodeSpec>();
                var ordinal = 1;
                foreach (var child in group.Expressions ?? [])
                {
                    var mapped = MapExpression(child, $"{path}.{ordinal}");
                    if (mapped is not null)
                        children.Add(mapped);
                    ordinal++;
                }

                return new QueryGroupSpec(path, group.Operator.ToString(), group.Negate, children);
            }

            case BinaryExpression binary:
            {
                var (kind, parameterName, value) = Value(binary.Right);
                return new QueryClauseSpec(
                    path,
                    binary.Left is FieldExpression field ? field.FieldName ?? string.Empty : string.Empty,
                    binary.Operator.ToString(),
                    kind,
                    parameterName,
                    value,
                    binary.Disabled);
            }

            case FullTextSearchExpression fullText:
                return new QueryFullTextSpec(
                    path,
                    fullText.Fields?.ToList() ?? [],
                    fullText.SearchText ?? string.Empty);

            default:
                return null;
        }
    }

    private static (ClauseValueKind Kind, string ParameterName, string Value) Value(DwExpression? right) => right switch
    {
        ParameterExpression parameter => (ClauseValueKind.Parameter, parameter.VariableName ?? string.Empty, string.Empty),
        MacroExpression macro => (ClauseValueKind.Macro, string.Empty, macro.LookupString ?? string.Empty),
        CodeExpression code => (ClauseValueKind.Code, string.Empty, Text(code)),
        ConstantExpression constant => (ClauseValueKind.Constant, string.Empty, Text(constant)),
        TermExpression term => (ClauseValueKind.Term, string.Empty, Text(term)),
        null => (ClauseValueKind.Unknown, string.Empty, string.Empty),
        _ => (ClauseValueKind.Unknown, string.Empty, Text(right))
    };

    private static string Text(DwExpression expression) => Safe(() => expression.ToString(), null) ?? string.Empty;

    // ---- facets ----------------------------------------------------------------------

    private FacetGroupSpec? MapFacets(string repository, string item)
    {
        var group = _queries.LoadFacets(repository, item);
        if (group is null)
            return null;

        var facets = (group.Items ?? [])
            .Where(f => f is not null)
            .Select(f => new FacetSpec(
                f.Name ?? string.Empty,
                f.Field ?? string.Empty,
                f.QueryParameter ?? string.Empty,
                f.TypeName ?? string.Empty,
                f.RenderType?.Name ?? string.Empty))
            .ToList();

        return new FacetGroupSpec(
            repository,
            item,
            Display(group.Name, item),
            group.Source?.Repository ?? string.Empty,
            group.Source?.Item ?? string.Empty,
            facets);
    }

    // ---- helpers ---------------------------------------------------------------------

    /// <summary>Repository items carry their file extension ("Products.index"); drop it for display.</summary>
    internal static string Display(string? name, string fallback)
    {
        var text = string.IsNullOrEmpty(name) ? fallback : name;
        foreach (var extension in new[] { ".index", ".query", ".facets" })
        {
            if (text.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                return text[..^extension.Length];
        }

        return text;
    }

    /// <summary>"Dynamicweb.Ecommerce.Indexing.ProductIndexBuilder, Dynamicweb.Ecommerce" -&gt; "ProductIndexBuilder".</summary>
    internal static string ShortType(string? typeName) => IndexFieldSpec.ShortenType(typeName);

    private static T Safe<T>(Func<T> read, T fallback)
    {
        try
        {
            return read();
        }
        catch
        {
            return fallback;
        }
    }
}
