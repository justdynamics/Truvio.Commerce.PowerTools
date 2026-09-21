namespace Truvio.Commerce.PowerTools.Features.BackendRights.Core;

/// <summary>
/// One gated thing in the admin tree, with everything both gates need. Permission values are
/// resolved by the source INSIDE the target user's permission context; capability values are pure
/// lookups that need no impersonation.
/// </summary>
/// <param name="PermissionKey">The permission section key — for an area, its display name.</param>
/// <param name="PermissionLevel">Resolved level, or null when no permission applies to this kind.</param>
/// <param name="DwSaysRestricted">
/// DW's own <c>UserHasCapability</c> answer, when it could be read. Kept beside the evaluator's own
/// computation so a disagreement can be reported rather than silently picked.
/// </param>
public sealed record RightsNodeSpec(
    RightsNodeKind Kind,
    string Id,
    string Name,
    int Sort,
    string ParentId,
    string PermissionKey,
    int? PermissionLevel,
    PermissionOrigin Origin,
    string InheritedFrom,
    string CapabilityKey,
    string LicenseFeature,
    bool LicenseOk,
    bool? DwSaysRestricted = null,
    int? RequiredLevel = null)
{
    public bool DeclaresCapability => !string.IsNullOrEmpty(CapabilityKey);

    public bool IsLicensed => string.IsNullOrEmpty(LicenseFeature) || LicenseOk;
}
