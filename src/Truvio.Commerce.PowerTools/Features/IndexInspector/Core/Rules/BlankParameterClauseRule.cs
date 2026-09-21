using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core.Rules;

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
                    "Give the parameter a default value, or make the clause a constant.",
                    // The parameter this finding is about — lets settings suppress it by name.
                    clause.ParameterName);
            }
        }
    }
}
