namespace Truvio.Commerce.PowerTools.Features.BackendRights.Core;

/// <summary>Which of the three gates produced the verdict.</summary>
public enum RightsGate
{
    /// <summary>Capability control was on and the thing declares a capability.</summary>
    Capability,

    /// <summary>Section permissions decided.</summary>
    Permission,

    /// <summary>The license does not carry the required feature.</summary>
    License,

    /// <summary>Angel / built-in administrator — no check ran at all.</summary>
    Bypass,

    /// <summary>Nothing gates it at composition time (see <see cref="RightsEvaluator"/> for sections).</summary>
    None
}
