namespace Truvio.Commerce.PowerTools.Features.BackendRights.Core;

/// <summary>One capability as declared by a <c>CapabilityProvider</c>.</summary>
/// <param name="RequiredCapabilities">
/// The cascade parents. Authoritative — the key STRING hierarchy is not: DW ships
/// <c>/Content/Settings</c> with no required capability while <c>/Content/Navigation</c> requires
/// <c>/Content</c>.
/// </param>
public sealed record CapabilityMeta(
    string Key,
    string Name,
    IReadOnlyList<string> RequiredCapabilities);
