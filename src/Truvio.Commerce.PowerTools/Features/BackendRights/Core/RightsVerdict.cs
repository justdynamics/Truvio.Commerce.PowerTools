namespace Truvio.Commerce.PowerTools.Features.BackendRights.Core;

/// <summary>One row of the report: what the user sees, which gate said so, and the evidence for both.</summary>
public sealed record RightsVerdict(
    RightsNodeSpec Node,
    bool Visible,
    RightsGate DecidedBy,
    CapabilityVerdict Capability,
    bool CapabilityConsulted,
    bool PermissionConsulted,
    bool PermissionGrantsRead,
    string Disagreement)
{
    public bool HasDisagreement => !string.IsNullOrEmpty(Disagreement);
}
