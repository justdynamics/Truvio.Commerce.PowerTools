using System.Globalization;
using Dynamicweb.Extensibility;
using Dynamicweb.Indexing;
using Dynamicweb.Indexing.Queries;
using Dynamicweb.Indexing.Querying;
using Dynamicweb.Indexing.Querying.Expressions;
using Dynamicweb.Indexing.Querying.Faceting;
using Truvio.Commerce.PowerTools.Core.Search.Testing;
using DwExpression = Dynamicweb.Indexing.Querying.Expressions.Expression;
using DwQuery = Dynamicweb.Indexing.Querying.Query;

namespace Truvio.Commerce.PowerTools.Core.Search.Dw;

/// <summary>One document of a test run, already rendered for display.</summary>
public sealed record RunDocument(int Ordinal, string Key, string Label, IReadOnlyList<DocumentField> Fields)
{
    public string? Value(string field) =>
        Fields.FirstOrDefault(f => string.Equals(f.Name, field, StringComparison.OrdinalIgnoreCase))?.Value;

    public IReadOnlyDictionary<string, string> AsDictionary()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in Fields)
            result[field.Name] = field.Value;
        return result;
    }
}

/// <summary>Facet buckets for one facet of one facet group.</summary>
public sealed record RunFacet(string Group, string Facet, string Field, IReadOnlyList<KeyValuePair<string, long>> Values);

/// <summary>The outcome of one execution of a repository query.</summary>
public sealed record QueryRunResult(
    int TotalHits,
    int Returned,
    double ElapsedMs,
    string Instance,
    string LuceneQuery,
    string Error,
    IReadOnlyList<RunDocument> Documents,
    IReadOnlyList<RunFacet> Facets)
{
    public static QueryRunResult Failed(string error) => new(0, 0, 0, string.Empty, string.Empty, error, [], []);

    public bool Ok => string.IsNullOrEmpty(Error);
}

/// <summary>
/// Runs a repository query against its live index instance, read-only, through the platform's
/// own <see cref="IndexQueryProvider"/> — the same path the frontend takes.
/// <para>
/// Every run reloads the query from disk (<c>QueryService.LoadQuery</c> does an
/// <c>XDocument.Load</c> per call and caches nothing), so a run may safely flip the
/// <c>Disabled</c> flag of one <see cref="BinaryExpression"/> to measure that clause's impact,
/// or reuse a sub-expression inside a freshly built <see cref="DwQuery"/>. Nothing is ever
/// written back: <c>SaveQuery</c> is never called.
/// </para>
/// <para>
/// Clause paths are the same strings <see cref="DwSearchSource.MapExpression"/> produces
/// ("1", "1.2", "1.2.3"), so a <see cref="Testing.ClauseTrace"/> and the live expression tree
/// address the same node.
/// </para>
/// </summary>
public static class DwQueryRunner
{
    /// <summary>Hard result cap — this is a diagnostic, not a data export.</summary>
    public const int MaxTake = 25;

    /// <summary>How many clauses are measured individually before the report gives up (each is a query).</summary>
    public const int MaxMeasuredClauses = 20;

    private static readonly string[] LabelFields = ["Name", "Title", "PageName", "UserName", "Number", "ProductName"];

    private static readonly string[] KeyFieldCandidates = ["ID", "PageID", "UserID", "AutoID", "ProductKey"];

    // ---- running -------------------------------------------------------------------------

    /// <summary>Runs the query as the frontend would, optionally with one clause switched off.</summary>
    public static QueryRunResult Run(
        string repository,
        string item,
        IReadOnlyDictionary<string, string> values,
        int take,
        string? disableClausePath = null,
        bool includeFacets = false)
    {
        return Execute(repository, item, values, take, query =>
        {
            if (!string.IsNullOrEmpty(disableClausePath))
            {
                var node = Find(query.Expression, "1", disableClausePath);
                if (node is BinaryExpression binary)
                    binary.Disabled = true;
            }

            return query.Expression;
        }, includeFacets);
    }

    /// <summary>Runs one clause on its own — how many documents does it match at all?</summary>
    public static QueryRunResult RunClauseAlone(
        string repository,
        string item,
        IReadOnlyDictionary<string, string> values,
        string clausePath)
    {
        return Execute(repository, item, values, 1,
            query => Find(query.Expression, "1", clausePath), includeFacets: false);
    }

    /// <summary>Runs one clause ANDed with a key lookup — does that one document pass it?</summary>
    public static QueryRunResult RunClauseForKey(
        string repository,
        string item,
        IReadOnlyDictionary<string, string> values,
        string clausePath,
        string keyField,
        string keyValue)
    {
        return Execute(repository, item, values, 1, query =>
        {
            var clause = Find(query.Expression, "1", clausePath);
            var key = ExpressionHelper.CreateFieldExpression(keyField, keyField, keyValue, OperatorType.Equal);
            return clause is null ? key : DwExpression.Group(false, OperatorType.And, [key, clause]);
        }, includeFacets: false);
    }

    /// <summary>Fetches one document of the query's source index by its key field.</summary>
    public static QueryRunResult FindByKey(string repository, string item, string keyField, string keyValue)
    {
        return Execute(repository, item, new Dictionary<string, string>(), 1,
            _ => ExpressionHelper.CreateFieldExpression(keyField, keyField, keyValue, OperatorType.Equal),
            includeFacets: false);
    }

    /// <summary>
    /// The field a document is addressed by in this index — "ID" on a product index, then the
    /// other built-in identity fields, then whatever the schema calls its first stored field.
    /// </summary>
    public static string KeyFieldFor(IndexSpec? index)
    {
        if (index is null)
            return "ID";

        foreach (var candidate in KeyFieldCandidates)
        {
            if (index.Field(candidate) is not null)
                return candidate;
        }

        return index.Fields.FirstOrDefault()?.SystemName ?? "ID";
    }

    /// <summary>
    /// Resolves the macro and code expressions of the query in the current (backend) context,
    /// keyed by clause path, so the pure diagnosis can report what they actually produced.
    /// A macro that only resolves on the frontend returns an empty string here — which is
    /// exactly the fact the report needs to show.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ResolveRuntimeValues(string repository, string item)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        IQuery? query;
        try
        {
            query = ServiceLocator.Current.GetInstance<IQueryService>().LoadQuery(repository, item);
        }
        catch
        {
            return result;
        }

        if (query is null)
            return result;

        Collect(query.Expression, "1");
        return result;

        void Collect(DwExpression? expression, string path)
        {
            switch (expression)
            {
                case GroupExpression group:
                {
                    var ordinal = 1;
                    foreach (var child in group.Expressions ?? [])
                    {
                        Collect(child, $"{path}.{ordinal}");
                        ordinal++;
                    }

                    return;
                }

                case BinaryExpression binary when binary.Right is MacroExpression or CodeExpression:
                {
                    string text;
                    try
                    {
                        text = DwIndexDocuments.Render(((ValueExpression)binary.Right).GetValue());
                    }
                    catch
                    {
                        text = string.Empty;
                    }

                    result[path] = text;
                    return;
                }
            }
        }
    }

    /// <summary>How many documents the online instance holds, for the narrowing comparison.</summary>
    public static int? IndexTotal(string repository, string item) => DwIndexDocuments.Count(repository, item);

    // ---- internals -----------------------------------------------------------------------

    private static QueryRunResult Execute(
        string repository,
        string item,
        IReadOnlyDictionary<string, string> values,
        int take,
        Func<IQuery, DwExpression?> shapeExpression,
        bool includeFacets)
    {
        if (string.IsNullOrEmpty(repository) || string.IsNullOrEmpty(item))
            return QueryRunResult.Failed("No query selected.");

        take = Math.Clamp(take <= 0 ? 10 : take, 1, MaxTake);

        IQuery? loaded;
        try
        {
            loaded = ServiceLocator.Current.GetInstance<IQueryService>().LoadQuery(repository, item);
        }
        catch (Exception ex)
        {
            return QueryRunResult.Failed($"The query could not be loaded: {ex.Message}");
        }

        if (loaded is null)
            return QueryRunResult.Failed($"Query '{repository}/{item}' was not found.");

        if (string.IsNullOrEmpty(loaded.Source?.Repository) || string.IsNullOrEmpty(loaded.Source?.Item))
            return QueryRunResult.Failed("The query has no source index, so it cannot run.");

        var sourceRepository = loaded.Source!.Repository;
        var sourceItem = loaded.Source!.Item;

        IIndex? index = null;
        try
        {
            index = ServiceLocator.Current.GetInstance<IIndexService>().LoadIndex(sourceRepository, sourceItem);
        }
        catch
        {
            // Reported through the run instead — the provider gives the real message.
        }

        DwExpression? expression;
        try
        {
            expression = shapeExpression(loaded);
        }
        catch (Exception ex)
        {
            return QueryRunResult.Failed($"The expression could not be prepared: {ex.Message}");
        }

        var facetGroups = includeFacets ? FacetGroupsFor(repository, item) : [];

        var query = new DwQuery
        {
            Name = loaded.Name ?? item,
            Meta = new Dictionary<string, string>(),
            Settings = new Dictionary<string, string>(),
            Parameters = loaded.Parameters?.ToList() ?? [],
            Imports = [],
            References = [],
            SortOrder = loaded.SortOrder?.ToList() ?? [],
            Source = new QuerySource { Repository = sourceRepository, Item = sourceItem },
            Expression = expression
        };

        var settings = new QuerySettings
        {
            Take = take,
            Skip = 0,
            IncludeDebugInfo = true,
            Parameters = Parameters(values),
            Facets = facetGroups.Count == 0 ? null! : facetGroups
        };

        IQueryResult? result;
        try
        {
            result = new IndexQueryProvider().Query(query, settings);
        }
        catch (Exception ex)
        {
            // ArgumentException here is the "field not in schema" case the trace predicts.
            return QueryRunResult.Failed($"The query failed: {ex.Message}");
        }

        if (result is null)
        {
            return QueryRunResult.Failed(
                "The online index instance is not available — build the index before running the query.");
        }

        var documents = new List<RunDocument>();
        var ordinal = 0;
        foreach (var entry in result.QueryResult ?? [])
        {
            if (entry is not IDictionary<string, object> document)
                continue;

            ordinal++;
            var fields = document
                .Select(pair => new DocumentField(pair.Key, DwIndexDocuments.Render(pair.Value)))
                .ToList();

            documents.Add(new RunDocument(ordinal, Key(document, ordinal), Label(fields), fields));
        }

        return new QueryRunResult(
            result.TotalCount,
            documents.Count,
            result.DebugInfo?.ExecutionTime.TotalMilliseconds ?? 0,
            Safe(() => index?.GetInstance()?.Name) ?? string.Empty,
            Shorten(result.DebugInfo?.QueryText),
            string.Empty,
            documents,
            Facets(facetGroups, result));
    }

    /// <summary>
    /// Only non-empty values are handed over. An empty string WOULD stay in the dictionary
    /// (<c>HandleParameters</c> leaves it) and <c>GetValueFromExpression</c> would then return
    /// "" instead of null, so the clause would compare against an empty string rather than
    /// disappearing — not what "no value" means anywhere else in the platform.
    /// </summary>
    private static IDictionary<string, object> Parameters(IReadOnlyDictionary<string, string> values)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in values)
        {
            if (!string.IsNullOrEmpty(pair.Value))
                result[pair.Key] = pair.Value;
        }

        return result;
    }

    private static IReadOnlyList<IFacetGroup> FacetGroupsFor(string repository, string item)
    {
        var groups = new List<IFacetGroup>();
        try
        {
            var service = ServiceLocator.Current.GetInstance<IQueryService>();
            var repositories = ServiceLocator.Current.GetInstance<Dynamicweb.Indexing.Repositories.IRepositoryService>();

            foreach (var repositoryItem in repositories.GetRepositoryItems(repository) ?? [])
            {
                if (repositoryItem?.Name is null ||
                    !"Facets".Equals(repositoryItem.TypeName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var group = service.LoadFacets(repository, repositoryItem.Name);
                if (group?.Source is not null &&
                    SearchKeys.Same(group.Source.Repository, repository) &&
                    SearchKeys.Same(group.Source.Item, item))
                {
                    groups.Add(group);
                }
            }
        }
        catch
        {
            return [];
        }

        return groups;
    }

    private static IReadOnlyList<RunFacet> Facets(IReadOnlyList<IFacetGroup> groups, IQueryResult result)
    {
        if (groups.Count == 0 || result.FacetGroupResult is null)
            return [];

        var facets = new List<RunFacet>();
        foreach (var group in groups)
        {
            if (group.Name is null || !result.FacetGroupResult.TryGetValue(group.Name, out var groupResult) || groupResult is null)
                continue;

            foreach (var facet in group.Items ?? [])
            {
                if (facet?.Name is null)
                    continue;

                var values = Safe(() => groupResult.GetFacetResults(facet.Name)) ?? [];
                var buckets = values
                    .Where(v => v is not null && v.Count > 0)
                    .OrderByDescending(v => v.Count)
                    .Take(10)
                    .Select(v => new KeyValuePair<string, long>(
                        string.IsNullOrEmpty(v.Label) ? DwIndexDocuments.Render(v.Value) : v.Label, v.Count))
                    .ToList();

                // Kept even when every bucket is empty: "this facet produces no values" is
                // exactly the finding a facet that faces an unindexed field gives you.
                facets.Add(new RunFacet(group.Name, facet.Name, facet.Field ?? string.Empty, buckets));
            }
        }

        return facets;
    }

    /// <summary>Finds the expression node the given clause path addresses.</summary>
    internal static DwExpression? Find(DwExpression? expression, string path, string wanted)
    {
        if (expression is null)
            return null;

        if (string.Equals(path, wanted, StringComparison.Ordinal))
            return expression;

        if (expression is not GroupExpression group)
            return null;

        // Only descend where the wanted path can still live.
        if (!wanted.StartsWith(path + ".", StringComparison.Ordinal))
            return null;

        var ordinal = 1;
        foreach (var child in group.Expressions ?? [])
        {
            var found = Find(child, $"{path}.{ordinal}", wanted);
            if (found is not null)
                return found;
            ordinal++;
        }

        return null;
    }

    private static string Key(IDictionary<string, object> document, int ordinal)
    {
        if (document.TryGetValue("ID", out var id) && id is not null)
        {
            var variant = document.TryGetValue("VariantID", out var v) ? DwIndexDocuments.Render(v) : string.Empty;
            var rendered = DwIndexDocuments.Render(id);
            return string.IsNullOrEmpty(variant) ? rendered : $"{rendered}/{variant}";
        }

        foreach (var candidate in KeyFieldCandidates)
        {
            if (document.TryGetValue(candidate, out var value) && value is not null)
                return DwIndexDocuments.Render(value);
        }

        return $"#{ordinal.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string Label(IReadOnlyList<DocumentField> fields)
    {
        foreach (var candidate in LabelFields)
        {
            var value = fields.FirstOrDefault(f => string.Equals(f.Name, candidate, StringComparison.OrdinalIgnoreCase))?.Value;
            if (!string.IsNullOrWhiteSpace(value))
                return QueryDiagnosis.Shorten(value, 90);
        }

        var fallback = fields.FirstOrDefault(f => !string.IsNullOrWhiteSpace(f.Value));
        return fallback is null ? string.Empty : QueryDiagnosis.Shorten(fallback.Value, 90);
    }

    /// <summary>
    /// The debug text is "&lt;lucene query&gt; TIMINGS &lt;per-stage timings&gt;" — keep the query,
    /// drop the timings, and cap it so a 33-clause query does not take over the report.
    /// </summary>
    private static string Shorten(string? queryText)
    {
        if (string.IsNullOrEmpty(queryText))
            return string.Empty;

        var marker = queryText.IndexOf(" TIMINGS ", StringComparison.Ordinal);
        var text = marker > 0 ? queryText[..marker] : queryText;
        return QueryDiagnosis.Shorten(text, 2000);
    }

    private static T? Safe<T>(Func<T?> read)
    {
        try
        {
            return read();
        }
        catch
        {
            return default;
        }
    }
}
