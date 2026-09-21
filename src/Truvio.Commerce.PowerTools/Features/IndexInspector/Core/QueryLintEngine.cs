using Truvio.Commerce.PowerTools.Features.IndexInspector.Core.Rules;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

/// <summary>Runs every registered lint rule over one catalog snapshot.</summary>
public sealed class QueryLintEngine
{
    private readonly IReadOnlyList<IQueryLintRule> _rules;

    public QueryLintEngine() : this(SearchRules.All())
    {
    }

    public QueryLintEngine(IReadOnlyList<IQueryLintRule> rules) => _rules = rules;

    public IReadOnlyList<Finding> Run(SearchCatalog catalog) =>
        _rules
            .SelectMany(rule => Safe(rule, catalog))
            .OrderBy(f => f.Severity switch
            {
                FindingSeverity.Critical => 0,
                FindingSeverity.Warning => 1,
                _ => 2
            })
            .ThenBy(f => f.RuleId, StringComparer.Ordinal)
            .ThenBy(f => f.EntityDisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(f => f.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public IReadOnlyList<Finding> Run(ISearchSource source) => Run(SearchCatalog.From(source));

    /// <summary>One broken rule must not take the whole report down.</summary>
    private static IEnumerable<Finding> Safe(IQueryLintRule rule, SearchCatalog catalog)
    {
        try
        {
            return rule.Evaluate(catalog).ToList();
        }
        catch (Exception ex)
        {
            return
            [
                new Finding(
                    rule.RuleId,
                    FindingSeverity.Info,
                    SearchEntityNames.Index,
                    rule.RuleId,
                    rule.RuleId,
                    "Rule could not be evaluated",
                    ex.Message)
            ];
        }
    }
}
