using Truvio.Commerce.PowerTools.Core.Diagnostics;

namespace Truvio.Commerce.PowerTools.Core.Search.Rules;

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

/// <summary>
/// IDX-W9 — a sort on a field that is not indexed. A field with <c>Indexed=false</c> is only
/// stored, so Lucene has no sortable term values for it (the index writer only adds an
/// indexed Lucene field when <c>FieldDefinitionBase.Indexed</c> is set).
/// </summary>
public sealed class UnsortableFieldRule : IQueryLintRule
{
    public string RuleId => "IDX-W9";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var query in catalog.Queries)
        {
            var index = catalog.IndexFor(query);
            if (index is null)
                continue;

            foreach (var sort in query.SortOrder)
            {
                if (string.IsNullOrEmpty(sort.Field) || MissingSortFieldRule.IsScore(sort.Field))
                    continue;

                var field = index.Field(sort.Field);
                if (field is null || field.Indexed)
                    continue;

                yield return new Finding(
                    RuleId,
                    FindingSeverity.Warning,
                    SearchEntityNames.Query,
                    query.Key,
                    catalog.Describe(query),
                    $"Sort field '{sort.Field}' is stored but not indexed",
                    $"Field '{sort.Field}' on index '{index.Name}' has Indexed=false, so it carries no sortable " +
                    "values and the sort has no effect. Set Indexed on the field, or sort on another field.");
            }
        }
    }
}
