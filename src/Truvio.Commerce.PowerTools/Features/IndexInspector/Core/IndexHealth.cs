namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

/// <summary>Health of an index, derived from its instances' last build status.</summary>
public enum IndexHealth
{
    Ok,
    Stale,
    NeverBuilt,
    Failed
}
