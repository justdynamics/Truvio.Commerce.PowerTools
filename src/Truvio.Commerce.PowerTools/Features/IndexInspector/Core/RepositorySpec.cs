namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

public sealed record RepositorySpec(
    string Name,
    string Description,
    IReadOnlyList<IndexSpec> Indexes,
    IReadOnlyList<QuerySpec> Queries,
    IReadOnlyList<FacetGroupSpec> FacetGroups);
