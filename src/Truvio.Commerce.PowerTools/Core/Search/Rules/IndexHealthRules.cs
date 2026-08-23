using System.Globalization;
using Truvio.Commerce.PowerTools.Core.Diagnostics;

namespace Truvio.Commerce.PowerTools.Core.Search.Rules;

/// <summary>IDX-W15 — two queries that would execute identically.</summary>
public sealed class DuplicateQueryRule : IQueryLintRule
{
    public string RuleId => "IDX-W15";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        var groups = catalog.Queries
            .GroupBy(Signature, StringComparer.Ordinal)
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            var members = group.OrderBy(q => q.Key, StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var query in members)
            {
                var others = members.Where(q => q != query).Select(catalog.Describe);

                yield return new Finding(
                    RuleId,
                    FindingSeverity.Info,
                    SearchEntityNames.Query,
                    query.Key,
                    catalog.Describe(query),
                    "Query is identical to another query",
                    $"Same source, expression and sort order as: {string.Join(", ", others)}. " +
                    "Keeping one of them removes a maintenance trap.");
            }
        }
    }

    /// <summary>Source + expression + sort order; names and descriptions deliberately excluded.</summary>
    internal static string Signature(QuerySpec query)
    {
        var expression = Describe(query.Expression);
        var sort = string.Join(",", query.SortOrder.Select(s => $"{s.Field}:{s.Direction}"));
        return $"{query.SourceKey.ToLowerInvariant()}|{expression}|{sort}";
    }

    private static string Describe(QueryNodeSpec? node) => node switch
    {
        null => string.Empty,
        QueryClauseSpec c => $"[{c.FieldName} {c.Operator} {c.ValueKind}:{c.ParameterName}{c.Value}{(c.Disabled ? " off" : "")}]",
        QueryGroupSpec g => $"({g.Operator}{(g.Negate ? "!" : "")}:{string.Join(",", g.Children.Select(Describe))})",
        QueryFullTextSpec f => $"<ft {string.Join("+", f.Fields)} {f.SearchText}>",
        _ => "?"
    };
}

/// <summary>IDX-W16 — an index no query reads from: it is built and maintained for nothing.</summary>
public sealed class UnusedIndexRule : IQueryLintRule
{
    public string RuleId => "IDX-W16";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var index in catalog.Indexes)
        {
            if (catalog.QueriesFor(index).Count > 0)
                continue;

            yield return new Finding(
                RuleId,
                FindingSeverity.Info,
                SearchEntityNames.Index,
                index.Key,
                catalog.Describe(index),
                "No query reads from this index",
                "Nothing in any repository points at the index. It is still rebuilt on schedule; delete it " +
                "or point a query at it.");
        }
    }
}

/// <summary>
/// IDX-W17 — the index has never been built, its last build failed, or it has not been
/// refreshed for a day. Mirrors the platform's own threshold: <c>IndexHelper</c> flags an
/// index as a warning once <c>lastBuildTime &lt; DateTime.Now.AddHours(-24)</c>.
/// </summary>
public sealed class IndexNotBuiltRule : IQueryLintRule
{
    public string RuleId => "IDX-W17";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var index in catalog.Indexes)
        {
            var (severity, title) = index.Health switch
            {
                IndexHealth.NeverBuilt => (FindingSeverity.Critical, "Index has never been built"),
                IndexHealth.Failed => (FindingSeverity.Critical, "Last index build failed"),
                IndexHealth.Stale => (FindingSeverity.Warning, "Index has not been rebuilt for over 24 hours"),
                _ => (FindingSeverity.Info, string.Empty)
            };

            if (title.Length == 0)
                continue;

            var when = index.LastBuild.HasValue
                ? $"Last build: {index.LastBuild.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}. "
                : "There is no build history for any instance. ";

            yield return new Finding(
                RuleId,
                severity,
                SearchEntityNames.Index,
                index.Key,
                catalog.Describe(index),
                title,
                when + index.HealthDetail + " Documents served from this index can be out of date.");
        }
    }
}

/// <summary>The rule set the linter runs, in rule-id order.</summary>
public static class SearchRules
{
    public static IReadOnlyList<IQueryLintRule> All() =>
    [
        new BlankParameterClauseRule(),
        new QueryMatchesEverythingRule(),
        new UndeclaredParameterRule(),
        new UnusedParameterRule(),
        new DisabledClauseRule(),
        new MissingQuerySourceRule(),
        new MissingExpressionFieldRule(),
        new MissingSortFieldRule(),
        new UnsortableFieldRule(),
        new MissingFacetSourceRule(),
        new MissingFacetFieldRule(),
        new UnindexedFacetFieldRule(),
        new AnalyzedFacetFieldRule(),
        new FacetParameterRule(),
        new DuplicateQueryRule(),
        new UnusedIndexRule(),
        new IndexNotBuiltRule()
    ];
}
