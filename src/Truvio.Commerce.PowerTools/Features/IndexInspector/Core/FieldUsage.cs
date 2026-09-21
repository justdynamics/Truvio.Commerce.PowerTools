namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

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
