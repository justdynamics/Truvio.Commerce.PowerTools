using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core.Rules;

/// <summary>
/// IDX-W13 — a facet on an analyzed field. Facet values are written per analyzer token for an
/// analyzed field, so the buckets are single words instead of the whole field value
/// ("Bosch Professional" becomes "bosch" + "professional").
/// </summary>
public sealed class AnalyzedFacetFieldRule : IQueryLintRule
{
    public string RuleId => "IDX-W13";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var group in catalog.FacetGroups)
        {
            var index = catalog.IndexFor(group);
            if (index is null)
                continue;

            foreach (var facet in group.Facets)
            {
                var field = index.Field(facet.Field);
                if (field is null || !field.Indexed || !field.Analyzed)
                    continue;

                yield return new Finding(
                    RuleId,
                    FindingSeverity.Info,
                    SearchEntityNames.Facets,
                    group.Key,
                    catalog.Describe(group),
                    $"Facet '{facet.Name}' uses analyzed field '{facet.Field}'",
                    "For an analyzed field the facet buckets are analyzer tokens, not whole values. " +
                    "Facet on a non-analyzed copy of the field if you want one bucket per value.");
            }
        }
    }
}
