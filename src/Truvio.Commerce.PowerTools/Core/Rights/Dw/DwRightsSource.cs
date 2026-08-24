using System.Reflection;
using Dynamicweb.CoreUI.Application;
using Dynamicweb.CoreUI.Navigation;
using Dynamicweb.Extensibility.AddIns;
using Dynamicweb.Security.Permissions;
using Dynamicweb.Security.UserManagement;
using Truvio.Commerce.PowerTools.Core.Permissions;

namespace Truvio.Commerce.PowerTools.Core.Rights.Dw;

/// <summary>
/// Builds one <see cref="RightsSnapshot"/> from the live install. The only file in the tool that
/// touches Dynamicweb; everything else is pure over the snapshot.
/// <para>
/// Strictly read-only. The one unusual thing it does is enumerate the admin tree INSIDE
/// <c>PermissionContext.BackendUserContext(user)</c>: DW's own <c>NavigationSection</c> constructor
/// resolves its area's permission level for whoever the context says is current, so building the
/// tree inside the target user's context is what makes the section and node rows describe THAT
/// user instead of the administrator running the report.
/// </para>
/// </summary>
public sealed class DwRightsSource
{
    /// <summary>Users listed by the picker per request.</summary>
    public const int DefaultUserCap = 500;

    public RightsSnapshot Build(int userId)
    {
        var user = UserManagementServices.Users.GetUserById(userId);
        if (user is null)
            return RightsSnapshot.Empty(new RightsSubject(userId, $"User {userId}", string.Empty, false, false, false, false));

        var subject = Subject(user);

        // Read once per report: it resolves a DI service on every call.
        var capabilityControlActive = CapabilityReflection.IsCapabilityControlActive();
        var capabilities = CapabilityReflection.GetCapabilities();
        var capabilityDataAvailable = CapabilityReflection.Available;

        var groupIds = SafeGroupIds(user);
        var limitations = Limitations(capabilities, capabilityDataAvailable);

        var nodes = new List<RightsNodeSpec>();
        try
        {
            // One tight scope. PermissionContext is an AsyncLocal stack that must be disposed in
            // reverse creation order, so nothing here awaits or calls foreign async code.
            using var context = PermissionContext.BackendUserContext(user);
            nodes.AddRange(ReadTree(subject));
        }
        catch
        {
            // A tree that cannot be read must still produce a report with the account facts.
        }

        return new RightsSnapshot(
            subject,
            capabilityControlActive,
            capabilityDataAvailable,
            CapabilityReflection.IsPermissionHierarchyActive() ?? false,
            nodes,
            capabilities,
            limitations,
            OwnerLevels(user),
            groupIds,
            OrphanedSectionKeys(nodes));
    }

    /// <summary>The backend users the picker offers.</summary>
    public (IReadOnlyList<RightsSubject> Users, int TotalCount) GetBackendUsers(string? search, bool includeWithoutAccess, int cap = DefaultUserCap)
    {
        try
        {
            var result = UserManagementServices.Users.GetUsersBySearch(new UserSearchFilter
            {
                SearchValue = search ?? string.Empty,
                PageNumber = 1,
                PageSize = cap
            });

            var users = result.Users
                .Select(Subject)
                .Where(s => includeWithoutAccess || s.AllowBackend)
                .OrderBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return (users, result.TotalCount);
        }
        catch
        {
            return ([], 0);
        }
    }

    private static RightsSubject Subject(User user) => new(
        user.ID,
        string.IsNullOrEmpty(user.Name) ? user.UserName : user.Name,
        user.UserName ?? string.Empty,
        SafeBool(() => user.GetAllowBackendWithInheritance()),
        SafeBool(() => user.IsAdmin),
        SafeBool(() => user.IsAngel),
        SafeBool(() => user.IsBuiltInAdmin));

    // ---- The admin tree ------------------------------------------------------------------------

    private static IEnumerable<RightsNodeSpec> ReadTree(RightsSubject subject)
    {
        var specs = new List<RightsNodeSpec>();

        foreach (var area in Areas())
        {
            var areaType = area.GetType().FullName ?? string.Empty;
            var areaId = $"area:{areaType}";
            var (level, origin, inheritedFrom) = AreaPermission(area);
            var capabilityKey = CapabilityReflection.CapabilityOf(area);
            var (licenseFeature, licenseOk) = License(area.GetType());

            specs.Add(new RightsNodeSpec(
                RightsNodeKind.Area,
                areaId,
                area.Name ?? areaType,
                area.Sort,
                ParentId: string.Empty,
                PermissionKey: area.Name ?? string.Empty,
                PermissionLevel: level,
                Origin: origin,
                InheritedFrom: inheritedFrom,
                CapabilityKey: capabilityKey,
                LicenseFeature: licenseFeature,
                LicenseOk: licenseOk,
                DwSaysRestricted: DwRestricted(subject, capabilityKey)));

            foreach (var section in Sections(areaType))
            {
                var sectionKey = string.IsNullOrWhiteSpace(section.Id)
                    ? section.GetType().FullName ?? string.Empty
                    : section.Name ?? string.Empty;
                var sectionId = $"section:{areaType}:{sectionKey}";
                var sectionCapability = CapabilityReflection.CapabilityOf(section);
                var (sectionLicense, sectionLicensed) = License(section.GetType());

                specs.Add(new RightsNodeSpec(
                    RightsNodeKind.Section,
                    sectionId,
                    section.Name ?? sectionKey,
                    section.Sort,
                    ParentId: areaId,
                    // A section carries its AREA's permission section — NavigationSection's
                    // constructor sets PermissionLevelCurrentUser from the area, never from a
                    // section-specific entity.
                    PermissionKey: area.Name ?? string.Empty,
                    PermissionLevel: CapabilityReflection.PermissionLevelOf(section) ?? level,
                    Origin: origin,
                    InheritedFrom: inheritedFrom,
                    CapabilityKey: sectionCapability,
                    LicenseFeature: sectionLicense,
                    LicenseOk: sectionLicensed,
                    DwSaysRestricted: DwRestricted(subject, sectionCapability)));

                foreach (var node in Nodes(section, sectionKey))
                {
                    var nodeCapability = CapabilityReflection.CapabilityOf(node);
                    specs.Add(new RightsNodeSpec(
                        RightsNodeKind.Node,
                        $"node:{sectionId}:{node.Id}",
                        node.Name ?? node.Id,
                        node.Sort,
                        ParentId: sectionId,
                        PermissionKey: area.Name ?? string.Empty,
                        PermissionLevel: CapabilityReflection.PermissionLevelOf(node),
                        Origin: origin,
                        InheritedFrom: inheritedFrom,
                        CapabilityKey: nodeCapability,
                        LicenseFeature: string.Empty,
                        LicenseOk: true,
                        DwSaysRestricted: DwRestricted(subject, nodeCapability),
                        RequiredLevel: CapabilityReflection.RequiredLevelOf(node)));
                }
            }
        }

        return specs;
    }

    private static IReadOnlyList<AreaBase> Areas()
    {
        try
        {
            return AddInManager.GetInstances<AreaBase>().OrderBy(a => a.Sort).ToList();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Mirrors <c>NavigationSectionProvider.GetSections()</c>: every <see cref="NavigationSection"/>
    /// add-in built with the area's navigation context, filtered to this area.
    /// </summary>
    private static IReadOnlyList<NavigationSection> Sections(string areaType)
    {
        try
        {
            var context = new NavigationContext { Value = NavigationContext.Empty };
            return AddInManager.GetInstances<NavigationSection>(context)
                .Where(s => s is not null && s.AreaType == areaType)
                .Where(SafeShouldShow)
                .OrderBy(s => s.Sort)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<NavigationNode> Nodes(NavigationSection section, string sectionKey)
    {
        try
        {
            var context = section.Context ?? new NavigationContext { Value = NavigationContext.Empty };
            var nodes = new List<NavigationNode>();

            foreach (var provider in AddInManager.GetInstances<NavigationNodeProvider>()
                         .Where(p => p is not null && p.SectionType == sectionKey))
            {
                try
                {
                    provider.SetContext(context);
                    nodes.AddRange(provider.GetRootNodes() ?? []);
                }
                catch
                {
                    // A provider that throws for this user contributes nothing, not a broken report.
                }
            }

            return nodes.Where(n => n is not null).OrderBy(n => n.Sort).ToList();
        }
        catch
        {
            return [];
        }
    }

    private static bool SafeShouldShow(NavigationSection section)
    {
        try
        {
            return section.ShouldShow();
        }
        catch
        {
            return false;
        }
    }

    // ---- Permissions ---------------------------------------------------------------------------

    /// <summary>
    /// The area's resolved level plus where it came from. The level itself is DW's own
    /// resolution inside the caller's <c>BackendUserContext</c>; the origin is derived from the
    /// context-free permission rows, which is the only way to tell explicit from inherited.
    /// </summary>
    private static (int? Level, PermissionOrigin Origin, string InheritedFrom) AreaPermission(AreaBase area)
    {
        try
        {
            var section = area.GetPermissionSection();
            var level = (int)section.GetPermission();
            var key = area.Name ?? string.Empty;

            var service = new PermissionService();
            var explicitRows = SafeRows(() => service.GetPermissionsByQuery(new PermissionQuery
            {
                Name = section.GetPermissionName(),
                Key = key
            }));

            if (explicitRows > 0)
                return (level, PermissionOrigin.Explicit, string.Empty);

            // '/'-delimited parents: "Content/Settings" inherits from "Content".
            var parent = key.Contains('/') ? key[..key.LastIndexOf('/')] : string.Empty;
            if (!string.IsNullOrEmpty(parent))
            {
                var parentRows = SafeRows(() => service.GetPermissionsByQuery(new PermissionQuery
                {
                    Name = section.GetPermissionName(),
                    Key = parent
                }));
                if (parentRows > 0)
                    return (level, PermissionOrigin.Inherited, parent);
            }

            return (level, Levels.GrantsRead(level) ? PermissionOrigin.RoleDefault : PermissionOrigin.ContextDefault, string.Empty);
        }
        catch
        {
            return (null, PermissionOrigin.NotEvaluated, string.Empty);
        }
    }

    private static int SafeRows(Func<IEnumerable<Permission>?> read)
    {
        try
        {
            return read()?.Count() ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// The backend owner priority, in DW's own order: direct groups, then their ancestors level by
    /// level, then the backend roles. The user's own id is never an owner here.
    /// </summary>
    private static IReadOnlyList<OwnerLevel> OwnerLevels(User user)
    {
        var levels = new List<OwnerLevel>();

        try
        {
            var groups = user.GetGroups()?.ToList() ?? [];
            if (groups.Count > 0)
            {
                levels.Add(new OwnerLevel(0, "Direct groups", groups
                    .Select(g => new OwnerSpec(g.ID.ToString(), g.Name ?? $"Group {g.ID}", "Group", null))
                    .ToList()));
            }

            // Ancestors, one priority level per generation.
            var current = groups;
            var seen = groups.Select(g => g.ID).ToHashSet();
            var level = 1;
            while (level < 20)
            {
                var parents = current
                    .Select(g => SafeParent(g))
                    .Where(g => g is not null && seen.Add(g.ID))
                    .Select(g => g!)
                    .ToList();

                if (parents.Count == 0)
                    break;

                levels.Add(new OwnerLevel(level, $"Ancestor groups (level {level})", parents
                    .Select(g => new OwnerSpec(g.ID.ToString(), g.Name ?? $"Group {g.ID}", "Group", null))
                    .ToList()));

                current = parents;
                level++;
            }
        }
        catch
        {
            // Membership that cannot be read still leaves the role level below.
        }

        var roles = new List<OwnerSpec>();
        if (SafeBool(() => user.IsAdmin))
            roles.Add(new OwnerSpec("Administrator", "Administrators (role)", "Role", Levels.All));
        roles.Add(new OwnerSpec("AuthenticatedBackend", "Authenticated users (backend)", "Role", null));

        levels.Add(new OwnerLevel(levels.Count, "Backend roles", roles));
        return levels;
    }

    private static UserGroup? SafeParent(UserGroup group)
    {
        try
        {
            return group.GetParentGroup();
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<int> SafeGroupIds(User user)
    {
        try
        {
            return UserManagementServices.UserGroups.GetGroupIdsByUserId(user.ID)?.ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Section permission rows whose key matches no installed area — the rename-orphan case. A
    /// section's permission key is the area's DISPLAY NAME, so renaming an area strands its rows.
    /// </summary>
    private static IReadOnlyList<string> OrphanedSectionKeys(IReadOnlyList<RightsNodeSpec> nodes)
    {
        try
        {
            var live = nodes
                .Where(n => n.Kind == RightsNodeKind.Area)
                .Select(n => n.PermissionKey)
                .Where(k => !string.IsNullOrEmpty(k))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (live.Count == 0)
                return [];

            var rows = new PermissionService().GetPermissionsByQuery(new PermissionQuery { Name = "Section" });
            return rows?
                .Select(r => r.Key)
                .Where(k => !string.IsNullOrEmpty(k))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                // A sub-section key ("Content/Settings") is live when its area is.
                .Where(k => !live.Contains(k) && !live.Contains(k.Split('/')[0]))
                .ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    // ---- Capabilities ---------------------------------------------------------------------------

    private static IReadOnlyList<CapabilityLimitationSpec> Limitations(IReadOnlyList<CapabilityMeta> capabilities, bool available)
    {
        if (!available || capabilities.Count == 0)
            return [];

        try
        {
            return CapabilityReflection.GetLimitations(capabilities.Select(c => c.Key))
                .Select(l => new CapabilityLimitationSpec(l.UserGroupId, GroupName(l.UserGroupId), l.Key))
                .OrderBy(l => l.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static string GroupName(int groupId)
    {
        try
        {
            return UserManagementServices.UserGroups.GetGroupById(groupId)?.Name ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>DW's own answer, for cross-checking. Null when it cannot be read.</summary>
    private static bool? DwRestricted(RightsSubject subject, string capabilityKey)
    {
        if (string.IsNullOrEmpty(capabilityKey))
            return null;

        return CapabilityReflection.UserHasCapability(subject.UserId, capabilityKey) is bool has ? !has : null;
    }

    // ---- License ----------------------------------------------------------------------------------

    /// <summary>
    /// The <c>LicensableAttribute</c> on an area, read reflectively: its constructor shape changed
    /// across versions (the old <c>Licensable(bool)</c> is obsolete-as-error at 10.28), and an area
    /// with no attribute is simply not license-gated. Unreadable = treated as licensed, so the
    /// column never invents a denial.
    /// </summary>
    private static (string Feature, bool Ok) License(Type type)
    {
        try
        {
            var attribute = type.GetCustomAttributes()
                .FirstOrDefault(a => a.GetType().Name == "LicensableAttribute");
            if (attribute is null)
                return (string.Empty, true);

            var feature = attribute.GetType().GetProperty("FeatureName")?.GetValue(attribute) as string;
            if (string.IsNullOrEmpty(feature))
                return (string.Empty, true);

            var managerType = typeof(AreaBase).Assembly.GetType("Dynamicweb.Security.Licensing.LicenseManager")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("Dynamicweb.Security.Licensing.LicenseManager"))
                    .FirstOrDefault(t => t is not null);

            var method = managerType?.GetMethod("LicenseHasFeature", BindingFlags.Public | BindingFlags.Static);
            var ok = method?.Invoke(null, [feature]) as bool?;
            return (feature, ok ?? true);
        }
        catch
        {
            return (string.Empty, true);
        }
    }

    private static bool SafeBool(Func<bool> read)
    {
        try
        {
            return read();
        }
        catch
        {
            return false;
        }
    }
}
