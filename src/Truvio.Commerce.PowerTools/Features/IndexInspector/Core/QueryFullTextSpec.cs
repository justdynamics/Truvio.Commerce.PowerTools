namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

public sealed record QueryFullTextSpec(
    string Path,
    IReadOnlyList<string> Fields,
    string SearchText) : QueryNodeSpec(Path);
