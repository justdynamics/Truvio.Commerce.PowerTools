using Truvio.Commerce.PowerTools.Features.IndexInspector.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core.Rules;

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
