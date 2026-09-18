using Truvio.Commerce.PowerTools.Features.IndexInspector.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core.Rules;

/// <summary>
/// IDX-W8 — a sort references a field that is not in the schema. Unlike a clause this fails
/// quietly: <c>LuceneQueryProvider.GetSort</c> skips (<c>continue</c>) any SortInfo whose
/// field is missing, so results come back in an unexpected order.
/// </summary>
public sealed class MissingSortFieldRule : IQueryLintRule
{
    public string RuleId => "IDX-W8";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var query in catalog.Queries)
        {
            var index = catalog.IndexFor(query);
            if (index is null)
                continue;

            foreach (var sort in query.SortOrder)
            {
                if (string.IsNullOrEmpty(sort.Field) || IsScore(sort.Field) || index.Field(sort.Field) is not null)
                    continue;

                yield return new Finding(
                    RuleId,
                    FindingSeverity.Warning,
                    SearchEntityNames.Query,
                    query.Key,
                    catalog.Describe(query),
                    $"Sort field '{sort.Field}' is not in the index schema",
                    $"Index '{index.Name}' has no field with that system name. The sort is silently ignored " +
                    "and results come back in relevance order instead.");
            }
        }
    }

    internal static bool IsScore(string field) =>
        string.Equals(field, "_score", StringComparison.OrdinalIgnoreCase);
}
