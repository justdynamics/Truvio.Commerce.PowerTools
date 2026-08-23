using Truvio.Commerce.PowerTools.Core.Diagnostics;

namespace Truvio.Commerce.PowerTools.Core.Search;

/// <summary>Entity names used on search findings, so the list can group by what was inspected.</summary>
public static class SearchEntityNames
{
    public const string Index = "Index";
    public const string Query = "Query";
    public const string Facets = "Facets";
}

/// <summary>
/// A single index/query misconfiguration detector. Rules are pure over the
/// <see cref="SearchCatalog"/> so they run against hand-built specs in tests.
/// </summary>
public interface IQueryLintRule
{
    string RuleId { get; }

    IEnumerable<Finding> Evaluate(SearchCatalog catalog);
}

/// <summary>Runs every registered lint rule over one catalog snapshot.</summary>
public sealed class QueryLintEngine
{
    private readonly IReadOnlyList<IQueryLintRule> _rules;

    public QueryLintEngine() : this(Rules.SearchRules.All())
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
