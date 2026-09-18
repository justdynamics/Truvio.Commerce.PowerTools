using System.Globalization;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core.Rules;

/// <summary>IDX-W15 — two queries that would execute identically.</summary>
public sealed class DuplicateQueryRule : IQueryLintRule
{
    public string RuleId => "IDX-W15";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        var groups = catalog.Queries
            .GroupBy(Signature, StringComparer.Ordinal)
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            var members = group.OrderBy(q => q.Key, StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var query in members)
            {
                var others = members.Where(q => q != query).Select(catalog.Describe);

                yield return new Finding(
                    RuleId,
                    FindingSeverity.Info,
                    SearchEntityNames.Query,
                    query.Key,
                    catalog.Describe(query),
                    "Query is identical to another query",
                    $"Same source, expression and sort order as: {string.Join(", ", others)}. " +
                    "Keeping one of them removes a maintenance trap.");
            }
        }
    }

    /// <summary>Source + expression + sort order; names and descriptions deliberately excluded.</summary>
    internal static string Signature(QuerySpec query)
    {
        var expression = Describe(query.Expression);
        var sort = string.Join(",", query.SortOrder.Select(s => $"{s.Field}:{s.Direction}"));
        return $"{query.SourceKey.ToLowerInvariant()}|{expression}|{sort}";
    }

    private static string Describe(QueryNodeSpec? node) => node switch
    {
        null => string.Empty,
        QueryClauseSpec c => $"[{c.FieldName} {c.Operator} {c.ValueKind}:{c.ParameterName}{c.Value}{(c.Disabled ? " off" : "")}]",
        QueryGroupSpec g => $"({g.Operator}{(g.Negate ? "!" : "")}:{string.Join(",", g.Children.Select(Describe))})",
        QueryFullTextSpec f => $"<ft {string.Join("+", f.Fields)} {f.SearchText}>",
        _ => "?"
    };
}
