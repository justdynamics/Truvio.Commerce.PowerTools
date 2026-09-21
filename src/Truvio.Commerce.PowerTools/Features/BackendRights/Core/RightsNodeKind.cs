namespace Truvio.Commerce.PowerTools.Features.BackendRights.Core;

/// <summary>Where in the admin tree a gated thing sits. Areas gate differently from sections and nodes.</summary>
public enum RightsNodeKind
{
    Area,
    Section,
    Node
}
