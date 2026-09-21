using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core.Rules;

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
