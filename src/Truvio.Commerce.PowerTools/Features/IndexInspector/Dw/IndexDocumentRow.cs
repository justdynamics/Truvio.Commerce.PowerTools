namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Dw;

public sealed record IndexDocumentRow(
    int Ordinal,
    string Key,
    IReadOnlyList<DocumentField> Fields,
    ProductMatch Match,
    IReadOnlyList<DocumentDifference> Differences)
{
    public string? Value(string field) =>
        Fields.FirstOrDefault(f => string.Equals(f.Name, field, StringComparison.OrdinalIgnoreCase))?.Value;
}
