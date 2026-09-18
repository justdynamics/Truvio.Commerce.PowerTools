namespace Truvio.Commerce.PowerTools.Features.BackendRights.Core;

/// <summary>Why a capability came out restricted — the difference matters to whoever has to fix it.</summary>
public enum CapabilityCause
{
    /// <summary>Not restricted.</summary>
    None,

    /// <summary>A group of the user carries a limitation row for this very key.</summary>
    Direct,

    /// <summary>No row on this key; a REQUIRED capability is restricted, so this one is too.</summary>
    Cascaded,

    /// <summary>Capability data could not be read on this host.</summary>
    Unknown
}
