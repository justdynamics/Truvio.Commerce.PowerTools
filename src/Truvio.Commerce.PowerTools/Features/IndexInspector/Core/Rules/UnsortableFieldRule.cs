using Truvio.Commerce.PowerTools.Features.IndexInspector.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core.Rules;

/// <summary>
/// IDX-W9 — a sort on a field that is not indexed. A field with <c>Indexed=false</c> is only
/// stored, so Lucene has no sortable term values for it (the index writer only adds an
/// indexed Lucene field when <c>FieldDefinitionBase.Indexed</c> is set).
/// </summary>
public sealed class UnsortableFieldRule : IQueryLintRule
{
    public string RuleId => "IDX-W9";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var query in catalog.Queries)
        {
            var index = catalog.IndexFor(query);
            if (index is null)
                continue;

            foreach (var sort in query.SortOrder)
            {
                if (string.IsNullOrEmpty(sort.Field) || MissingSortFieldRule.IsScore(sort.Field))
                    continue;

                var field = index.Field(sort.Field);
                if (field is null || field.Indexed)
                    continue;

                yield return new Finding(
                    RuleId,
                    FindingSeverity.Warning,
                    SearchEntityNames.Query,
                    query.Key,
                    catalog.Describe(query),
                    $"Sort field '{sort.Field}' is stored but not indexed",
                    $"Field '{sort.Field}' on index '{index.Name}' has Indexed=false, so it carries no sortable " +
                    "values and the sort has no effect. Set Indexed on the field, or sort on another field.");
            }
        }
    }
}
