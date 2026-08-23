using Truvio.Commerce.PowerTools.Core.Search;

namespace Truvio.Commerce.PowerTools.Tests;

/// <summary>Small builders so the search rule tests read like the repository they describe.</summary>
internal static class SearchSpecBuilders
{
    public const string Repo = "Products";
    public const string IndexItem = "Products.index";
    public const string QueryItem = "Products.query";
    public const string FacetsItem = "Products.facets";

    public static IndexFieldSpec Field(
        string systemName,
        string type = "System.String",
        bool stored = true,
        bool indexed = true,
        bool analyzed = false) =>
        new(systemName, systemName, type, string.Empty, string.Empty, stored, indexed, analyzed,
            string.Empty, string.Empty, "FieldDefinition");

    public static IndexSpec Index(
        IEnumerable<IndexFieldSpec>? fields = null,
        IndexHealth health = IndexHealth.Ok,
        string item = IndexItem,
        string repository = Repo,
        DateTime? lastBuild = null) =>
        new(
            repository,
            item,
            item.Replace(".index", string.Empty),
            "LastUpdated",
            "ProductIndexBuilder",
            (fields ?? [Field("Name"), Field("Active", "System.Boolean")]).ToList(),
            [new IndexInstanceSpec("Primary", "LuceneIndexProvider", true, true, "Completed", lastBuild, null)],
            [],
            health,
            "detail",
            lastBuild ?? DateTime.Now,
            "Primary");

    public static QueryClauseSpec Clause(
        string field,
        string op = "Equal",
        ClauseValueKind kind = ClauseValueKind.Constant,
        string parameter = "",
        string value = "x",
        bool disabled = false,
        string path = "1.1") =>
        new(path, field, op, kind, parameter, value, disabled);

    public static QueryClauseSpec ParameterClause(string field, string parameter, string op = "Equal", string path = "1.1") =>
        new(path, field, op, ClauseValueKind.Parameter, parameter, string.Empty, false);

    public static QueryGroupSpec And(params QueryNodeSpec[] children) => new("1", "And", false, children.ToList());

    public static QueryGroupSpec Or(params QueryNodeSpec[] children) => new("1", "Or", false, children.ToList());

    public static QuerySpec Query(
        QueryNodeSpec? expression,
        IEnumerable<QueryParameterSpec>? parameters = null,
        IEnumerable<QuerySortSpec>? sortOrder = null,
        string item = QueryItem,
        string sourceItem = IndexItem,
        string sourceRepository = Repo,
        string repository = Repo) =>
        new(
            repository,
            item,
            item.Replace(".query", string.Empty),
            string.Empty,
            sourceRepository,
            sourceItem,
            (parameters ?? []).ToList(),
            (sortOrder ?? []).ToList(),
            expression);

    public static QueryParameterSpec Parameter(string name, string? defaultValue = null) =>
        new(name, "System.String", defaultValue ?? string.Empty);

    public static FacetGroupSpec Facets(
        IEnumerable<FacetSpec> facets,
        string item = FacetsItem,
        string sourceItem = QueryItem,
        string repository = Repo) =>
        new(repository, item, item.Replace(".facets", string.Empty), repository, sourceItem, facets.ToList());

    public static FacetSpec Facet(string name, string field, string queryParameter) =>
        new(name, field, queryParameter, "Field", "Checkboxes");

    public static SearchCatalog Catalog(
        IEnumerable<IndexSpec>? indexes = null,
        IEnumerable<QuerySpec>? queries = null,
        IEnumerable<FacetGroupSpec>? facetGroups = null) =>
        new([
            new RepositorySpec(
                Repo,
                string.Empty,
                (indexes ?? [Index()]).ToList(),
                (queries ?? []).ToList(),
                (facetGroups ?? []).ToList())
        ]);
}
