namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

public sealed record QueryGroupSpec(
    string Path,
    string Operator,
    bool Negate,
    IReadOnlyList<QueryNodeSpec> Children) : QueryNodeSpec(Path)
{
    /// <summary>An And-group ANDs its children (Lucene Occur.MUST); anything else ORs them.</summary>
    public bool IsAnd => string.Equals(Operator, "And", StringComparison.OrdinalIgnoreCase);
}
