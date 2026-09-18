using Truvio.Commerce.PowerTools.Features.IndexInspector.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core.Rules;

/// <summary>
/// IDX-W7 — a clause names a field that the source index schema does not contain. The
/// provider throws for this case: <c>"The given field name does not exist in the given index
/// schema."</c> (Dynamicweb.Indexing.Lucene4 <c>Helpers.ParseQueryExpressionInternal</c>).
/// </summary>
public sealed class MissingExpressionFieldRule : IQueryLintRule
{
    public string RuleId => "IDX-W7";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var query in catalog.Queries)
        {
            var index = catalog.IndexFor(query);
            if (index is null)
                continue;

            foreach (var clause in query.Clauses())
            {
                if (clause.Disabled || string.IsNullOrEmpty(clause.FieldName) || index.Field(clause.FieldName) is not null)
                    continue;

                yield return new Finding(
                    RuleId,
                    FindingSeverity.Critical,
                    SearchEntityNames.Query,
                    query.Key,
                    catalog.Describe(query),
                    $"Clause field '{clause.FieldName}' is not in the index schema",
                    $"Index '{index.Name}' has no field with that system name. The index provider throws " +
                    "\"The given field name does not exist in the given index schema\" when the query runs.");
            }
        }
    }
}
