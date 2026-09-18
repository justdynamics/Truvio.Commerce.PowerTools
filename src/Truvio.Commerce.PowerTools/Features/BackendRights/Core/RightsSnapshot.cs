namespace Truvio.Commerce.PowerTools.Features.BackendRights.Core;

/// <summary>
/// Everything one report needs, read once. Pure data so the evaluator and the rules are testable
/// with no DW host.
/// </summary>
/// <param name="CapabilityControlActive">
/// <c>CapabilityHelper.IsCapabilityControlActive()</c>, read ONCE per report — it resolves a DI
/// service on every call.
/// </param>
/// <param name="CapabilityDataAvailable">
/// False when the capability API or its tables could not be read (a host below DW 10.19, or a
/// solution whose <c>CapabilityLimitation</c> table was never created). The report degrades to
/// "capability data unavailable" instead of failing.
/// </param>
/// <param name="PermissionHierarchyActive">
/// <c>PermissionHierarchyFeature</c>. It changes what <c>HasPermission</c> means, so a verdict that
/// differs between the two interpretations is reported rather than guessed.
/// </param>
public sealed record RightsSnapshot(
    RightsSubject Subject,
    bool CapabilityControlActive,
    bool CapabilityDataAvailable,
    bool PermissionHierarchyActive,
    IReadOnlyList<RightsNodeSpec> Nodes,
    IReadOnlyList<CapabilityMeta> Capabilities,
    IReadOnlyList<CapabilityLimitationSpec> Limitations,
    IReadOnlyList<OwnerLevel> OwnerLevels,
    IReadOnlyList<int> UserGroupIds,
    IReadOnlyList<string> OrphanedSectionKeys)
{
    public static RightsSnapshot Empty(RightsSubject subject) =>
        new(subject, false, false, false, [], [], [], [], [], []);
}
