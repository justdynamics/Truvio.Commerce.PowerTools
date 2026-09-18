using Truvio.Commerce.PowerTools.Features.IndexInspector.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core.Rules;

/// <summary>IDX-W6 — the query's source does not point at an index that exists.</summary>
public sealed class MissingQuerySourceRule : IQueryLintRule
{
    public string RuleId => "IDX-W6";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var query in catalog.Queries)
        {
            if (catalog.IndexFor(query) is not null)
                continue;

            var source = string.IsNullOrEmpty(query.SourceKey) ? "(none)" : query.SourceKey;

            yield return new Finding(
                RuleId,
                FindingSeverity.Critical,
                SearchEntityNames.Query,
                query.Key,
                catalog.Describe(query),
                $"Source index '{source}' does not exist",
                "The query points at an index that is not in any repository, so it cannot execute. " +
                "Repoint the query source at a live index.");
        }
    }
}
