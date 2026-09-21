namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

public sealed record FacetGroupSpec(
    string Repository,
    string Item,
    string Name,
    string SourceRepository,
    string SourceItem,
    IReadOnlyList<FacetSpec> Facets)
{
    public string Key => SearchKeys.For(Repository, Item);

    public string SourceKey => SearchKeys.For(SourceRepository, SourceItem);
}
