namespace Truvio.Commerce.PowerTools.Core.Search;

/// <summary>How a field is referenced from outside the schema.</summary>
public enum FieldUsageKind
{
    Expression,
    Sort,
    Facet
}

public sealed record FieldUsageReference(FieldUsageKind Kind, string Owner, string Detail);

/// <summary>
/// One row of the "field where-used" report: a field of an index, with every query clause,
/// sort and facet that names it.
/// </summary>
public sealed record FieldUsage(
    string Repository,
    string IndexItem,
    string IndexName,
    string FieldName,
    IndexFieldSpec? Field,
    IReadOnlyList<FieldUsageReference> References)
{
    /// <summary>The field is referenced somewhere but the index schema has no such field.</summary>
    public bool Dangling => Field is null;

    /// <summary>The field is in the schema and searchable, but nothing ever asks for it.</summary>
    public bool Dead => Field is not null && References.Count == 0 && Field.Indexed;

    public int ExpressionCount => References.Count(r => r.Kind == FieldUsageKind.Expression);

    public int SortCount => References.Count(r => r.Kind == FieldUsageKind.Sort);

    public int FacetCount => References.Count(r => r.Kind == FieldUsageKind.Facet);

    public string Status => Dangling ? "Dangling" : Dead ? "Unused" : References.Count > 0 ? "Used" : "Stored only";

    /// <summary>Short "2 queries, 1 facet" summary for the list column.</summary>
    public string UsageSummary()
    {
        var parts = new List<string>(3);
        if (ExpressionCount > 0)
            parts.Add(ExpressionCount == 1 ? "1 clause" : $"{ExpressionCount} clauses");
        if (SortCount > 0)
            parts.Add(SortCount == 1 ? "1 sort" : $"{SortCount} sorts");
        if (FacetCount > 0)
            parts.Add(FacetCount == 1 ? "1 facet" : $"{FacetCount} facets");
        return parts.Count == 0 ? "-" : string.Join(", ", parts);
    }
}

/// <summary>Builds the where-used report from a catalog. Pure, so it is unit-tested directly.</summary>
public static class FieldUsageMap
{
    public static IReadOnlyList<FieldUsage> Build(SearchCatalog catalog)
    {
        var rows = new List<FieldUsage>();

        foreach (var index in catalog.Indexes)
        {
            var references = new Dictionary<string, List<FieldUsageReference>>(StringComparer.OrdinalIgnoreCase);

            void Add(string? field, FieldUsageKind kind, string owner, string detail)
            {
                if (string.IsNullOrEmpty(field))
                    return;
                if (!references.TryGetValue(field, out var list))
                    references[field] = list = [];
                list.Add(new FieldUsageReference(kind, owner, detail));
            }

            foreach (var query in catalog.QueriesFor(index))
            {
                foreach (var clause in query.Clauses())
                {
                    var value = clause.ValueKind == ClauseValueKind.Parameter
                        ? "@" + clause.ParameterName
                        : clause.ValueKind.ToString();
                    Add(clause.FieldName, FieldUsageKind.Expression, query.Name,
                        $"{clause.Operator} {value}{(clause.Disabled ? " (disabled)" : string.Empty)}");
                }

                foreach (var sort in query.SortOrder)
                {
                    if (!string.Equals(sort.Field, "_score", StringComparison.OrdinalIgnoreCase))
                        Add(sort.Field, FieldUsageKind.Sort, query.Name, sort.Direction);
                }
            }

            foreach (var group in catalog.FacetGroupsFor(index))
            foreach (var facet in group.Facets)
                Add(facet.Field, FieldUsageKind.Facet, group.Name, facet.Name);

            foreach (var field in index.Fields)
            {
                references.TryGetValue(field.SystemName, out var list);
                rows.Add(new FieldUsage(
                    index.Repository, index.Item, index.Name, field.SystemName, field,
                    Sorted(list)));
            }

            // Anything referenced that the schema does not have: a dangling reference.
            foreach (var pair in references)
            {
                if (index.Field(pair.Key) is not null)
                    continue;

                rows.Add(new FieldUsage(
                    index.Repository, index.Item, index.Name, pair.Key, null, Sorted(pair.Value)));
            }
        }

        return rows
            .OrderBy(r => r.Repository, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.IndexName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.FieldName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<FieldUsageReference> Sorted(List<FieldUsageReference>? list) =>
        list is null
            ? []
            : list.OrderBy(r => r.Kind).ThenBy(r => r.Owner, StringComparer.OrdinalIgnoreCase).ToList();
}
