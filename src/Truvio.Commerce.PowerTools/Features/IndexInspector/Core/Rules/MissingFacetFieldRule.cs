using Truvio.Commerce.PowerTools.Features.IndexInspector.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core.Rules;

/// <summary>
/// IDX-W11 — a facet on a field the index schema does not have. <c>DoFacetSearch</c> looks the
/// field up in the schema and silently skips the facet when it is not found, so the facet is
/// simply never rendered.
/// </summary>
public sealed class MissingFacetFieldRule : IQueryLintRule
{
    public string RuleId => "IDX-W11";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var group in catalog.FacetGroups)
        {
            var index = catalog.IndexFor(group);
            if (index is null)
                continue;

            foreach (var facet in group.Facets)
            {
                if (string.IsNullOrEmpty(facet.Field) || index.Field(facet.Field) is not null)
                    continue;

                yield return new Finding(
                    RuleId,
                    FindingSeverity.Warning,
                    SearchEntityNames.Facets,
                    group.Key,
                    catalog.Describe(group),
                    $"Facet '{facet.Name}' uses field '{facet.Field}', which is not in the index schema",
                    $"Index '{index.Name}' has no field with that system name, so the facet is skipped and " +
                    "never appears in the result.");
            }
        }
    }
}
