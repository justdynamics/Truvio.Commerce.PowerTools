namespace Truvio.Commerce.PowerTools.Features.BackendRights.Core;

/// <summary>The capability side of one key, for one user.</summary>
public sealed record CapabilityVerdict(
    string Key,
    bool Restricted,
    CapabilityCause Cause,
    IReadOnlyList<string> CausingGroups,
    string CausingKey)
{
    public static CapabilityVerdict Allowed(string key) => new(key, false, CapabilityCause.None, [], string.Empty);
}
