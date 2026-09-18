using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core.Rules;

/// <summary>
/// IDX-W12 — a facet on a field with <c>Indexed=false</c>. The index writer only emits the
/// facet doc-values (<c>AddFacetFields</c>) when the field definition is indexed, so the
/// facet always comes back empty.
/// </summary>
public sealed class UnindexedFacetFieldRule : IQueryLintRule
{
    public string RuleId => "IDX-W12";

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
                if (field is null || field.Indexed)
                    continue;

                yield return new Finding(
                    RuleId,
                    FindingSeverity.Warning,
                    SearchEntityNames.Facets,
                    group.Key,
                    catalog.Describe(group),
                    $"Facet '{facet.Name}' uses field '{facet.Field}', which is not indexed",
                    "Facet values are only written for indexed fields, so this facet always comes back empty. " +
                    "Set Indexed on the field and rebuild the index.");
            }
        }
    }
}
