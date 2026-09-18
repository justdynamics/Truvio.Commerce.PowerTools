namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Dw;

public sealed record DocumentBrowseResult(
    int TotalCount,
    IReadOnlyList<IndexDocumentRow> Documents,
    string Error)
{
    public static DocumentBrowseResult Failed(string error) => new(0, [], error);
}
