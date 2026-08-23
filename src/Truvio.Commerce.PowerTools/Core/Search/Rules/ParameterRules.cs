using Truvio.Commerce.PowerTools.Core.Diagnostics;

namespace Truvio.Commerce.PowerTools.Core.Search.Rules;

/// <summary>
/// IDX-W1 — a clause compared against a parameter that has no default value. When nothing
/// supplies the parameter the clause is dropped from the executed query (verified:
/// <c>LuceneQueryProvider.HandleParameters</c> only seeds parameters with a non-empty
/// DefaultValue, and <c>Helpers.ParseQueryExpressionInternal</c> returns null for a clause
/// whose value resolves to null).
/// </summary>
public sealed class BlankParameterClauseRule : IQueryLintRule
{
    public string RuleId => "IDX-W1";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var query in catalog.Queries)
        {
            // A query that collapses entirely is the far bigger problem; IDX-W2 reports it.
            if (LuceneSemantics.Collapses(query))
                continue;

            foreach (var (clause, parent) in LuceneSemantics.ClausesWithParent(query))
            {
                if (clause.Disabled || clause.ValueKind != ClauseValueKind.Parameter)
                    continue;

                if (!LuceneSemantics.IsDroppable(query, clause))
                    continue;

                if (query.Parameter(clause.ParameterName) is null)
                    continue; // undeclared — IDX-W3 owns that case

                yield return new Finding(
                    RuleId,
                    FindingSeverity.Warning,
                    SearchEntityNames.Query,
                    query.Key,
                    catalog.Describe(query),
                    $"Clause '{clause}' vanishes when '{clause.ParameterName}' is not supplied",
                    $"Parameter '{clause.ParameterName}' has no default value, so unless every caller passes it " +
                    $"the clause is removed from the executed query and {LuceneSemantics.DropEffect(parent)}. " +
                    "Give the parameter a default value, or make the clause a constant.");
            }
        }
    }
}

/// <summary>
/// IDX-W2 — every clause in the query can disappear, so the whole expression resolves to
/// nothing and the provider falls back to <c>new MatchAllDocsQuery()</c>: the query returns
/// every document in the index.
/// </summary>
public sealed class QueryMatchesEverythingRule : IQueryLintRule
{
    public string RuleId => "IDX-W2";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var query in catalog.Queries)
        {
            if (!LuceneSemantics.Collapses(query))
                continue;

            var detail = query.Expression is null
                ? "The query has no expression at all."
                : $"None of its {query.Clauses().Count()} clause(s) survive: every one is either disabled or " +
                  "compares against a parameter with no default value.";

            yield return new Finding(
                RuleId,
                FindingSeverity.Critical,
                SearchEntityNames.Query,
                query.Key,
                catalog.Describe(query),
                "Query can return every document in the index",
                detail + " When the expression resolves to nothing the index provider falls back to a " +
                "match-all query, so the caller silently gets the whole index.");
        }
    }
}

/// <summary>
/// IDX-W3 — a clause references a parameter that the query does not declare. Only declared
/// parameters are ever seeded (<c>HandleParameters</c> iterates <c>query.Parameters</c>), so
/// the clause is dropped on every single request.
/// </summary>
public sealed class UndeclaredParameterRule : IQueryLintRule
{
    public string RuleId => "IDX-W3";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var query in catalog.Queries)
        foreach (var (clause, parent) in LuceneSemantics.ClausesWithParent(query))
        {
            if (clause.ValueKind != ClauseValueKind.Parameter || clause.Disabled)
                continue;

            if (string.IsNullOrEmpty(clause.ParameterName) || query.Parameter(clause.ParameterName) is not null)
                continue;

            yield return new Finding(
                RuleId,
                FindingSeverity.Critical,
                SearchEntityNames.Query,
                query.Key,
                catalog.Describe(query),
                $"Clause '{clause}' uses undeclared parameter '{clause.ParameterName}'",
                "The query does not declare this parameter, so it is never given a value and the clause is " +
                $"dropped from every execution — {LuceneSemantics.DropEffect(parent)}.");
        }
    }
}

/// <summary>IDX-W4 — a declared parameter that no clause and no facet ever uses.</summary>
public sealed class UnusedParameterRule : IQueryLintRule
{
    public string RuleId => "IDX-W4";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var query in catalog.Queries)
        {
            var used = new HashSet<string>(
                query.Clauses()
                    .Where(c => c.ValueKind == ClauseValueKind.Parameter && !string.IsNullOrEmpty(c.ParameterName))
                    .Select(c => c.ParameterName),
                StringComparer.OrdinalIgnoreCase);

            foreach (var group in catalog.FacetGroupsForQuery(query))
            foreach (var facet in group.Facets)
            {
                if (!string.IsNullOrEmpty(facet.QueryParameter))
                    used.Add(facet.QueryParameter);
            }

            foreach (var parameter in query.Parameters)
            {
                if (used.Contains(parameter.Name))
                    continue;

                yield return new Finding(
                    RuleId,
                    FindingSeverity.Info,
                    SearchEntityNames.Query,
                    query.Key,
                    catalog.Describe(query),
                    $"Parameter '{parameter.Name}' is never used",
                    "No clause in the expression and no facet on this query references the parameter. " +
                    "Passing it has no effect.");
            }
        }
    }
}

/// <summary>IDX-W5 — a clause left switched off in the query editor; it is skipped at runtime.</summary>
public sealed class DisabledClauseRule : IQueryLintRule
{
    public string RuleId => "IDX-W5";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var query in catalog.Queries)
        foreach (var clause in query.Clauses())
        {
            if (!clause.Disabled)
                continue;

            yield return new Finding(
                RuleId,
                FindingSeverity.Info,
                SearchEntityNames.Query,
                query.Key,
                catalog.Describe(query),
                $"Clause '{clause}' is disabled",
                "The clause is marked Disabled, so the provider skips it. Remove it if it is no longer wanted.");
        }
    }
}
