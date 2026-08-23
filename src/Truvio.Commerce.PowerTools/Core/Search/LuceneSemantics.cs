namespace Truvio.Commerce.PowerTools.Core.Search;

/// <summary>
/// The runtime semantics the linter reasons about, all verified against the shipped
/// index provider (Dynamicweb.Indexing.Lucene4, 10.8.4):
/// <list type="bullet">
/// <item><c>LuceneQueryProvider.HandleParameters</c> only seeds a parameter value when the
/// query's <c>QueryParameter.DefaultValue</c> is non-empty (or the caller supplied one).</item>
/// <item><c>Helpers.GetValueFromExpression</c> returns null for a ParameterExpression whose
/// name is absent from the parameter dictionary.</item>
/// <item><c>Helpers.ParseQueryExpressionInternal</c> returns null for that clause
/// (<c>if (value == null &amp;&amp; op != IsEmpty) return null;</c>) and a GroupExpression skips
/// null children, so a group whose children all vanish is itself null.</item>
/// <item><c>LuceneIndexProvider</c> then does
/// <c>ParseQueryExpression(...) ?? new MatchAllDocsQuery()</c> — a fully collapsed query
/// returns EVERY document in the index.</item>
/// </list>
/// </summary>
public static class LuceneSemantics
{
    /// <summary>
    /// True when the clause silently disappears from the executed query: it is disabled, or
    /// it compares against a parameter that carries no default value (and so is absent from
    /// the parameter dictionary unless a caller supplies it on every single request).
    /// </summary>
    public static bool IsDroppable(QuerySpec query, QueryClauseSpec clause)
    {
        if (clause.Disabled)
            return true;

        if (clause.ValueKind != ClauseValueKind.Parameter)
            return false;

        // IsEmpty is the one operator that survives a null value.
        if (string.Equals(clause.Operator, "IsEmpty", StringComparison.OrdinalIgnoreCase))
            return false;

        var parameter = query.Parameter(clause.ParameterName);
        return parameter is null || !parameter.HasDefault;
    }

    /// <summary>True when nothing at all is left of the expression tree — the query matches every document.</summary>
    public static bool Collapses(QuerySpec query) => CollapsesNode(query, query.Expression);

    public static bool CollapsesNode(QuerySpec query, QueryNodeSpec? node) => node switch
    {
        null => true,
        QueryClauseSpec clause => IsDroppable(query, clause),
        QueryGroupSpec group => group.Children.Count == 0 || group.Children.All(c => CollapsesNode(query, c)),
        QueryFullTextSpec fullText => string.IsNullOrWhiteSpace(fullText.SearchText),
        _ => false
    };

    /// <summary>Walks the expression tree yielding every clause together with the group that holds it.</summary>
    public static IEnumerable<(QueryClauseSpec Clause, QueryGroupSpec? Parent)> ClausesWithParent(QuerySpec query) =>
        Walk(query.Expression, null);

    private static IEnumerable<(QueryClauseSpec, QueryGroupSpec?)> Walk(QueryNodeSpec? node, QueryGroupSpec? parent)
    {
        switch (node)
        {
            case QueryClauseSpec clause:
                yield return (clause, parent);
                break;
            case QueryGroupSpec group:
                foreach (var child in group.Children)
                foreach (var result in Walk(child, group))
                    yield return result;
                break;
        }
    }

    /// <summary>
    /// A dropped clause inside an And-group removes a MUST constraint, so the result set grows
    /// (the dangerous direction); inside an Or-group it removes a SHOULD, so the result shrinks.
    /// </summary>
    public static string DropEffect(QueryGroupSpec? parent) =>
        parent is null || parent.IsAnd
            ? "the constraint disappears and the query returns MORE documents than intended"
            : "the alternative disappears and the query returns FEWER documents than intended";
}
