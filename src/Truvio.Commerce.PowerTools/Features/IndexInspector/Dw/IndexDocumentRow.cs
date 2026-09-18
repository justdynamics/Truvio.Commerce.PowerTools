using System.Collections;
using System.Globalization;
using Dynamicweb.Indexing;
using Dynamicweb.Indexing.Queries;
using Dynamicweb.Indexing.Querying;
using Dynamicweb.Indexing.Querying.Expressions;
using DwQuery = Dynamicweb.Indexing.Querying.Query;

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
