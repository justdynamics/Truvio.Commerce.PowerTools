namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

public sealed record FacetSpec(
    string Name,
    string Field,
    string QueryParameter,
    string TypeName,
    string RenderType);
