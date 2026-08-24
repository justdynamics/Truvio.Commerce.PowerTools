namespace Truvio.Commerce.PowerTools.Core.Rights;

/// <summary>Where in the admin tree a gated thing sits. Areas gate differently from sections and nodes.</summary>
public enum RightsNodeKind
{
    Area,
    Section,
    Node
}

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

/// <summary>How a permission level was arrived at, for the explanation.</summary>
public enum PermissionOrigin
{
    Explicit,
    Inherited,
    RoleDefault,
    ContextDefault,
    NotEvaluated
}

/// <summary>
/// The backend user the report is about. <paramref name="AllowBackend"/> is
/// <c>GetAllowBackendWithInheritance()</c> — false means the user cannot reach the admin at all,
/// whatever else is granted.
/// </summary>
public sealed record RightsSubject(
    int UserId,
    string DisplayName,
    string UserName,
    bool AllowBackend,
    bool IsAdmin,
    bool IsAngel,
    bool IsBuiltInAdmin)
{
    /// <summary>Angel and built-in admin skip BOTH gates; the Administrator user type skips only permissions.</summary>
    public bool IsElevated => IsAngel || IsBuiltInAdmin;

    public string StatusName =>
        !AllowBackend ? "No access"
        : IsElevated ? "Elevated"
        : IsAdmin ? "Administrator"
        : "Standard";
}

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

/// <summary>
/// One row of <c>CapabilityLimitation</c>: a DENY, keyed by user group. No row means allowed, and
/// there are no per-user or per-role rows.
/// </summary>
public sealed record CapabilityLimitationSpec(int UserGroupId, string GroupName, string Key)
{
    /// <summary>True when the group id no longer resolves to a live group.</summary>
    public bool GroupMissing => string.IsNullOrEmpty(GroupName);
}

/// <summary>
/// One priority level of the backend permission-owner chain, in resolution order: the user's direct
/// groups, then their ancestors level by level, then the backend roles. The user's own id is never
/// an owner in the backend.
/// </summary>
public sealed record OwnerLevel(int Level, string Description, IReadOnlyList<OwnerSpec> Owners);

/// <param name="DefaultLevel">
/// The owner's <c>DefaultPermission</c>, or null when it declares none —
/// <c>AuthenticatedBackend</c>'s null default is why backend access is grant-only.
/// </param>
public sealed record OwnerSpec(string Id, string DisplayName, string Kind, int? DefaultLevel);

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
