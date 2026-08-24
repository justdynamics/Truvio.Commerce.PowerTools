using Truvio.Commerce.PowerTools.Core.Permissions;
using Truvio.Commerce.PowerTools.Core.Rights;

namespace Truvio.Commerce.PowerTools.Tests;

/// <summary>
/// Builders for backend-rights snapshots. Every rule and the evaluator are pure over
/// <see cref="RightsSnapshot"/>, so the whole tool is testable with no DW host.
/// </summary>
internal static class BackendRightsTestData
{
    public const string ContentKey = "/Content";
    public const string NavigationKey = "/Content/Navigation";
    public const string SettingsKey = "/Content/Settings";

    public static RightsSubject Standard(bool allowBackend = true) =>
        new(17, "Jane Doe", "jane", allowBackend, IsAdmin: false, IsAngel: false, IsBuiltInAdmin: false);

    public static RightsSubject Administrator() =>
        new(18, "Adam Admin", "adam", true, IsAdmin: true, IsAngel: false, IsBuiltInAdmin: false);

    public static RightsSubject BuiltInAdmin() =>
        new(1, "Administrator", "admin", true, IsAdmin: true, IsAngel: false, IsBuiltInAdmin: true);

    public static RightsSubject Angel() =>
        new(2, "System", "angel", true, IsAdmin: false, IsAngel: true, IsBuiltInAdmin: false);

    /// <summary>An area row. Read + a capability by default; override per test.</summary>
    public static RightsNodeSpec Area(
        string name = "Content",
        int? level = Levels.Read,
        string capability = ContentKey,
        PermissionOrigin origin = PermissionOrigin.Explicit,
        string licenseFeature = "",
        bool licenseOk = true,
        bool? dwSaysRestricted = null) =>
        new(RightsNodeKind.Area, $"area:{name}", name, 10, string.Empty, name, level, origin,
            string.Empty, capability, licenseFeature, licenseOk, dwSaysRestricted);

    public static RightsNodeSpec Section(
        string name = "Navigation",
        string parentId = "area:Content",
        string permissionKey = "Content",
        int? level = Levels.Read,
        string capability = NavigationKey) =>
        new(RightsNodeKind.Section, $"section:{name}", name, 10, parentId, permissionKey, level,
            PermissionOrigin.Explicit, string.Empty, capability, string.Empty, true);

    public static RightsNodeSpec Node(
        string name = "Pages",
        string parentId = "section:Navigation",
        string permissionKey = "Content",
        int? level = Levels.Read,
        string capability = "",
        int? requiredLevel = null) =>
        new(RightsNodeKind.Node, $"node:{name}", name, 10, parentId, permissionKey, level,
            PermissionOrigin.Explicit, string.Empty, capability, string.Empty, true, null, requiredLevel);

    /// <summary>DW's stock shape: /Content/Navigation requires /Content, /Content/Settings does not.</summary>
    public static IReadOnlyList<CapabilityMeta> Capabilities() =>
    [
        new(ContentKey, "Content", []),
        new(NavigationKey, "Navigation", [ContentKey]),
        new(SettingsKey, "Settings", [])
    ];

    public static RightsSnapshot Snapshot(
        RightsSubject? subject = null,
        bool capabilityControlActive = false,
        bool capabilityDataAvailable = true,
        IReadOnlyList<RightsNodeSpec>? nodes = null,
        IReadOnlyList<CapabilityLimitationSpec>? limitations = null,
        IReadOnlyList<CapabilityMeta>? capabilities = null,
        IReadOnlyList<int>? groupIds = null,
        IReadOnlyList<OwnerLevel>? owners = null,
        IReadOnlyList<string>? orphanedSectionKeys = null) =>
        new(subject ?? Standard(),
            capabilityControlActive,
            capabilityDataAvailable,
            PermissionHierarchyActive: false,
            nodes ?? [Area()],
            capabilities ?? Capabilities(),
            limitations ?? [],
            owners ?? [],
            groupIds ?? [42],
            orphanedSectionKeys ?? []);

    /// <summary>A deny row for one of the user's groups (group 42 unless told otherwise).</summary>
    public static CapabilityLimitationSpec Limit(string key, int groupId = 42, string groupName = "Editors") =>
        new(groupId, groupName, key);

    public static RightsVerdict For(RightsSnapshot snapshot, string nodeName) =>
        RightsEvaluator.Evaluate(snapshot).First(v => v.Node.Name == nodeName);
}
