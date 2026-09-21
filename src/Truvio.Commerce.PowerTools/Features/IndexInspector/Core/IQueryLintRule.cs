using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

/// <summary>
/// A single index/query misconfiguration detector. Rules are pure over the
/// <see cref="SearchCatalog"/> so they run against hand-built specs in tests.
/// </summary>
public interface IQueryLintRule
{
    string RuleId { get; }

    IEnumerable<Finding> Evaluate(SearchCatalog catalog);
}
