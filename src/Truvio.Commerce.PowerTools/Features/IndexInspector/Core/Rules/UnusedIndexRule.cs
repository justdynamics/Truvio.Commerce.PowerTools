using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core.Rules;

/// <summary>IDX-W16 — an index no query reads from: it is built and maintained for nothing.</summary>
public sealed class UnusedIndexRule : IQueryLintRule
{
    public string RuleId => "IDX-W16";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var index in catalog.Indexes)
        {
            if (catalog.QueriesFor(index).Count > 0)
                continue;

            yield return new Finding(
                RuleId,
                FindingSeverity.Info,
                SearchEntityNames.Index,
                index.Key,
                catalog.Describe(index),
                "No query reads from this index",
                "Nothing in any repository points at the index. It is still rebuilt on schedule; delete it " +
                "or point a query at it.");
        }
    }
}
