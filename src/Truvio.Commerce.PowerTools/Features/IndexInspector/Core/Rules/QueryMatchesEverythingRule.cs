using Truvio.Commerce.PowerTools.Features.IndexInspector.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core.Rules;

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
                "match-all query, so the caller silently gets the whole index.",
                // The parameters whose missing defaults collapse the query — suppressible by name.
                string.Join(",", query.Clauses()
                    .Where(c => c.ValueKind == ClauseValueKind.Parameter && !string.IsNullOrEmpty(c.ParameterName))
                    .Select(c => c.ParameterName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)));
        }
    }
}
