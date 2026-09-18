using Truvio.Commerce.PowerTools.Features.IndexInspector.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core.Rules;

/// <summary>
/// IDX-W14 — a facet whose query parameter is missing or is not declared on the source query.
/// <c>DoFacetSearch</c> skips a facet with a null query parameter outright, and a selection on
/// a parameter the query does not declare never reaches the expression.
/// </summary>
public sealed class FacetParameterRule : IQueryLintRule
{
    public string RuleId => "IDX-W14";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var group in catalog.FacetGroups)
        {
            var query = catalog.Query(group.SourceKey);
            if (query is null)
                continue;

            foreach (var facet in group.Facets)
            {
                if (string.IsNullOrEmpty(facet.QueryParameter))
                {
                    yield return new Finding(
                        RuleId,
                        FindingSeverity.Warning,
                        SearchEntityNames.Facets,
                        group.Key,
                        catalog.Describe(group),
                        $"Facet '{facet.Name}' has no query parameter",
                        "A facet without a query parameter is skipped when the facets are built, so it never renders.");
                    continue;
                }

                if (query.Parameter(facet.QueryParameter) is not null)
                    continue;

                yield return new Finding(
                    RuleId,
                    FindingSeverity.Warning,
                    SearchEntityNames.Facets,
                    group.Key,
                    catalog.Describe(group),
                    $"Facet '{facet.Name}' filters on parameter '{facet.QueryParameter}', which query '{query.Name}' does not declare",
                    "Selecting a value in this facet cannot narrow the result: the query never receives the parameter.");
            }
        }
    }
}
