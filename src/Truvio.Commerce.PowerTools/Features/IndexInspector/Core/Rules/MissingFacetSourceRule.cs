using Truvio.Commerce.PowerTools.Features.IndexInspector.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core.Rules;

/// <summary>IDX-W10 — the facet group's source query does not exist.</summary>
public sealed class MissingFacetSourceRule : IQueryLintRule
{
    public string RuleId => "IDX-W10";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var group in catalog.FacetGroups)
        {
            if (catalog.Query(group.SourceKey) is not null)
                continue;

            var source = string.IsNullOrEmpty(group.SourceKey) ? "(none)" : group.SourceKey;

            yield return new Finding(
                RuleId,
                FindingSeverity.Warning,
                SearchEntityNames.Facets,
                group.Key,
                catalog.Describe(group),
                $"Source query '{source}' does not exist",
                "The facet group points at a query that is not in any repository, so it can never be filled.");
        }
    }
}
