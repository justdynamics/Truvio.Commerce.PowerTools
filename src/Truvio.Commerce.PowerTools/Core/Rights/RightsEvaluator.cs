using Truvio.Commerce.PowerTools.Core.Permissions;

namespace Truvio.Commerce.PowerTools.Core.Rights;

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

/// <summary>
/// The rules DW itself applies, reproduced over a snapshot so they can be explained and tested.
/// <para>
/// The gate is chosen per node kind, and the three kinds genuinely differ (verified in the 10.27.9
/// decompile):
/// </para>
/// <list type="bullet">
/// <item><b>Area</b> — <c>ShellScreen.GetAreas()</c>: capability decides when the flag is on AND the
/// area declares one, otherwise the section permission must grant Read. License applies on top.</item>
/// <item><b>Section</b> — <c>NavigationByPathQuery.GetSectionResult</c>: permissions are consulted
/// only when the flag is OFF, and even then <c>ProcessPermissions()</c> filters the section's child
/// nodes and context actions, never the section itself. A section's own visibility below area level
/// therefore comes from <c>ShouldShow()</c> plus render-time capability filtering — so with the flag
/// ON a section that declares no capability is gated by nothing at composition time.</item>
/// <item><b>Node</b> — <c>GetNodeResult</c>: same either/or as areas, and a node is additionally
/// dropped when the parent's level does not satisfy the node's own
/// <c>PermissionLevelRequired</c> (default Read).</item>
/// </list>
/// </summary>
public static class RightsEvaluator
{
    /// <summary>Every row of the report, in tree order.</summary>
    public static IReadOnlyList<RightsVerdict> Evaluate(RightsSnapshot snapshot)
    {
        var verdicts = new List<RightsVerdict>();
        var byId = new Dictionary<string, RightsVerdict>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in snapshot.Nodes)
        {
            var verdict = EvaluateNode(snapshot, node, byId);
            verdicts.Add(verdict);
            byId[node.Id] = verdict;
        }

        return verdicts;
    }

    /// <summary>Areas only — the count the info bar reports as "N of M".</summary>
    public static (int Visible, int Total) AreaCount(IReadOnlyList<RightsVerdict> verdicts)
    {
        var areas = verdicts.Where(v => v.Node.Kind == RightsNodeKind.Area).ToList();
        return (areas.Count(a => a.Visible), areas.Count);
    }

    private static RightsVerdict EvaluateNode(
        RightsSnapshot snapshot,
        RightsNodeSpec node,
        IReadOnlyDictionary<string, RightsVerdict> resolved)
    {
        var subject = snapshot.Subject;

        // Hard gate first: no backend access means nothing is reachable, whatever is granted.
        if (!subject.AllowBackend)
        {
            return new RightsVerdict(node, false, RightsGate.Permission, CapabilityVerdict.Allowed(node.CapabilityKey),
                false, false, false, string.Empty);
        }

        var capability = ResolveCapability(snapshot, node.CapabilityKey);

        // Elevated users skip BOTH gates. The Administrator user TYPE does not — it only picks up
        // the Administrator role's All default on the permission side.
        if (subject.IsElevated)
        {
            return new RightsVerdict(node, node.IsLicensed, node.IsLicensed ? RightsGate.Bypass : RightsGate.License,
                CapabilityVerdict.Allowed(node.CapabilityKey), false, false, true, string.Empty);
        }

        var capabilityDecides = node.Kind != RightsNodeKind.Section
            && snapshot.CapabilityControlActive
            && node.DeclaresCapability;

        // Sections: permissions are consulted only while the flag is off, and never to hide the
        // section itself — so with the flag on, capability (render-time) is all that remains.
        var sectionUnderCapabilityControl = node.Kind == RightsNodeKind.Section && snapshot.CapabilityControlActive;

        var permissionGrantsRead = EffectiveLevel(node, resolved) is int level && Levels.GrantsRead(level);
        var disagreement = Disagreement(snapshot, node, capability);

        if (capabilityDecides || sectionUnderCapabilityControl)
        {
            var restricted = capability.Restricted;
            var visible = !restricted && node.IsLicensed && ParentVisible(node, resolved);
            var gate = restricted ? RightsGate.Capability
                : !node.IsLicensed ? RightsGate.License
                : sectionUnderCapabilityControl && !node.DeclaresCapability ? RightsGate.None
                : RightsGate.Capability;

            return new RightsVerdict(node, visible, gate, capability,
                CapabilityConsulted: true, PermissionConsulted: false, permissionGrantsRead, disagreement);
        }

        // Permission decides. A node additionally needs its parent's level to satisfy its own
        // required level (PermissionLevelExtension.WithPermission).
        var satisfiesRequired = SatisfiesRequiredLevel(node, resolved);
        var permitted = permissionGrantsRead && satisfiesRequired;
        var licensed = node.IsLicensed;
        var visibleByPermission = permitted && licensed && ParentVisible(node, resolved);

        return new RightsVerdict(node, visibleByPermission,
            !permitted ? RightsGate.Permission : !licensed ? RightsGate.License : RightsGate.Permission,
            capability, CapabilityConsulted: false, PermissionConsulted: true, permissionGrantsRead, disagreement);
    }

    /// <summary>
    /// The level the permission gate checks. A row that carries no level of its own is judged by
    /// its nearest ancestor's: DW resolves <c>PermissionLevelCurrentUser</c> only on some node
    /// types, and <c>GetNodeResult</c> gates a node on the parent's level — a missing own level is
    /// not a denial.
    /// </summary>
    private static int? EffectiveLevel(RightsNodeSpec node, IReadOnlyDictionary<string, RightsVerdict> resolved)
    {
        if (node.PermissionLevel is int own)
            return own;

        return !string.IsNullOrEmpty(node.ParentId) && resolved.TryGetValue(node.ParentId, out var parent)
            ? EffectiveLevel(parent.Node, resolved)
            : null;
    }

    /// <summary>A child of something the user cannot see is unreachable regardless of its own gates.</summary>
    private static bool ParentVisible(RightsNodeSpec node, IReadOnlyDictionary<string, RightsVerdict> resolved) =>
        string.IsNullOrEmpty(node.ParentId)
        || !resolved.TryGetValue(node.ParentId, out var parent)
        || parent.Visible;

    private static bool SatisfiesRequiredLevel(RightsNodeSpec node, IReadOnlyDictionary<string, RightsVerdict> resolved)
    {
        if (node.Kind != RightsNodeKind.Node || node.RequiredLevel is not int required)
            return true;

        // The level compared against is the PARENT's (a section carries its area's level).
        var parentLevel = !string.IsNullOrEmpty(node.ParentId) && resolved.TryGetValue(node.ParentId, out var parent)
            ? parent.Node.PermissionLevel
            : node.PermissionLevel;

        return parentLevel is not int level || (level & required) == required;
    }

    /// <summary>
    /// Is this key restricted for the user, and why? Mirrors
    /// <c>DefaultCapabilityService.IsCapabilityLimitedForUser</c>: an unknown key is never limited,
    /// elevated users are never limited, a restricted REQUIRED capability cascades, and any one of
    /// the user's groups carrying a row is enough.
    /// </summary>
    public static CapabilityVerdict ResolveCapability(RightsSnapshot snapshot, string key) =>
        ResolveCapability(snapshot, key, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    private static CapabilityVerdict ResolveCapability(RightsSnapshot snapshot, string key, HashSet<string> seen)
    {
        if (string.IsNullOrEmpty(key))
            return CapabilityVerdict.Allowed(key);

        if (!snapshot.CapabilityDataAvailable)
            return new CapabilityVerdict(key, false, CapabilityCause.Unknown, [], string.Empty);

        // Angel / built-in admin are never capability-limited (CapabilityHelper.IsRelevantUser).
        if (snapshot.Subject.IsElevated)
            return CapabilityVerdict.Allowed(key);

        // A key no provider declares is not a capability at all, so it limits nobody.
        var meta = snapshot.Capabilities.FirstOrDefault(c => Same(c.Key, key));
        if (meta is null)
            return CapabilityVerdict.Allowed(key);

        // Cycle guard: a malformed provider could declare a requirement loop.
        if (!seen.Add(key))
            return CapabilityVerdict.Allowed(key);

        // Direct denies first — they name the group to fix.
        var groups = snapshot.Limitations
            .Where(l => Same(l.Key, key) && snapshot.UserGroupIds.Contains(l.UserGroupId))
            .Select(l => string.IsNullOrEmpty(l.GroupName) ? $"group {l.UserGroupId}" : l.GroupName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (groups.Count > 0)
            return new CapabilityVerdict(key, true, CapabilityCause.Direct, groups, key);

        // Then the cascade — RequiredCapabilities, never the key string's own hierarchy.
        foreach (var required in meta.RequiredCapabilities)
        {
            var parent = ResolveCapability(snapshot, required, seen);
            if (parent.Restricted)
                return new CapabilityVerdict(key, true, CapabilityCause.Cascaded, parent.CausingGroups, parent.CausingKey);
        }

        return CapabilityVerdict.Allowed(key);
    }

    /// <summary>
    /// DW's own answer against ours. Reported rather than resolved: if they differ the report is
    /// reading something the evaluator does not model, and saying so beats picking a side.
    /// </summary>
    private static string Disagreement(RightsSnapshot snapshot, RightsNodeSpec node, CapabilityVerdict capability)
    {
        if (node.DwSaysRestricted is not bool dw || capability.Cause == CapabilityCause.Unknown)
            return string.Empty;

        if (dw == capability.Restricted)
            return string.Empty;

        return dw
            ? $"Dynamicweb reports {node.CapabilityKey} as restricted for this user, but no limitation row explains it — a capability provider or cache may hold state this report cannot see."
            : $"Dynamicweb reports {node.CapabilityKey} as allowed although a limitation row matches one of the user's groups — the capability cache may be stale.";
    }

    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
