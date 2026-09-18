using Truvio.Commerce.PowerTools.Features.IndexInspector.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core.Rules;

/// <summary>IDX-W5 — a clause left switched off in the query editor; it is skipped at runtime.</summary>
public sealed class DisabledClauseRule : IQueryLintRule
{
    public string RuleId => "IDX-W5";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var query in catalog.Queries)
        foreach (var clause in query.Clauses())
        {
            if (!clause.Disabled)
                continue;

            yield return new Finding(
                RuleId,
                FindingSeverity.Info,
                SearchEntityNames.Query,
                query.Key,
                catalog.Describe(query),
                $"Clause '{clause}' is disabled",
                "The clause is marked Disabled, so the provider skips it. Remove it if it is no longer wanted.");
        }
    }
}
