using Truvio.Commerce.PowerTools.Core.Permissions;

namespace Truvio.Commerce.PowerTools.Core.Rights;

/// <summary>
/// Turns one verdict into the sentences a person can act on: name the gate that decided, say why
/// the other one did not, and name who <i>does</i> get in. Pure — the wording is unit-tested.
/// </summary>
public static class RightsExplanation
{
    /// <summary>The one-line summary shown in the report's "Why?" column tooltip and panel heading.</summary>
    public static string Headline(RightsVerdict verdict)
    {
        var node = verdict.Node;
        var what = $"{Kind(node.Kind)} '{node.Name}'";

        return verdict switch
        {
            { DecidedBy: RightsGate.Bypass } => $"{what} is visible — the account bypasses every check.",
            { Visible: false, DecidedBy: RightsGate.License } => $"{what} is hidden — the license does not include {node.LicenseFeature}.",
            { Visible: false, DecidedBy: RightsGate.Capability } => $"{what} is hidden by a capability limitation.",
            { Visible: false, DecidedBy: RightsGate.Permission } => $"{what} is hidden — no permission grants Read.",
            { Visible: false } => $"{what} is hidden.",
            _ => $"{what} is visible."
        };
    }

    /// <summary>The full explanation, one sentence per fact, in reading order.</summary>
    public static IReadOnlyList<string> Sentences(RightsSnapshot snapshot, RightsVerdict verdict)
    {
        var lines = new List<string>();
        var node = verdict.Node;
        var subject = snapshot.Subject;
        var what = $"{Kind(node.Kind)} '{node.Name}'";

        if (!subject.AllowBackend)
        {
            lines.Add($"{subject.DisplayName} has no backend access: 'Allow backend' is off on the account and on every group it inherits from, so the administration is closed to this user entirely.");
            lines.Add("Nothing below is reachable regardless of the permissions or capabilities granted.");
            return lines;
        }

        if (subject.IsElevated)
        {
            lines.Add(subject.IsBuiltInAdmin
                ? "Built-in administrator — bypasses every permission check."
                : "System account (Angel) — bypasses every permission check.");
            lines.Add("Capability limitations are also skipped for this user, so nothing in the admin is hidden from it.");
            if (!node.IsLicensed)
                lines.Add($"{what} is still hidden: the license does not include feature '{node.LicenseFeature}', and the license gate applies to everyone.");
            return lines;
        }

        // ---- Capability side ----------------------------------------------------------------
        if (verdict.CapabilityConsulted)
        {
            lines.AddRange(CapabilitySentences(snapshot, verdict, what));
        }
        else if (snapshot.CapabilityControlActive && !node.DeclaresCapability)
        {
            lines.Add($"Capability control is on, but {what} declares no capability, so capabilities do not gate it.");
        }
        else if (!snapshot.CapabilityControlActive && node.DeclaresCapability)
        {
            lines.Add($"{what} declares capability {node.CapabilityKey}, but capability control is switched off, so that declaration has no effect today.");
        }

        // ---- Permission side ------------------------------------------------------------------
        if (verdict.PermissionConsulted)
        {
            lines.AddRange(PermissionSentences(snapshot, verdict, what));
        }
        else if (verdict.CapabilityConsulted)
        {
            lines.Add(node.PermissionLevel is int level
                ? $"Section permissions are not consulted for {what} while capability control is on — the {Levels.Name(level)} level on section '{node.PermissionKey}' has no effect today."
                : $"Section permissions are not consulted for {what} while capability control is on.");
        }

        // ---- License ---------------------------------------------------------------------------
        if (!node.IsLicensed)
            lines.Add($"The license does not include feature '{node.LicenseFeature}', which hides {what} even where the gates allow it.");

        if (verdict.HasDisagreement)
            lines.Add(verdict.Disagreement);

        return lines;
    }

    private static IEnumerable<string> CapabilitySentences(RightsSnapshot snapshot, RightsVerdict verdict, string what)
    {
        var node = verdict.Node;
        var capability = verdict.Capability;

        if (capability.Cause == CapabilityCause.Unknown)
        {
            yield return $"Capability control is on and {what} requires {node.CapabilityKey}, but the capability data could not be read on this host, so the verdict is unknown.";
            yield break;
        }

        if (!node.DeclaresCapability)
        {
            yield return $"Capability control is on. {what} declares no capability of its own; with the flag on, section permissions are not consulted for sections either, so nothing gates it at composition time.";
            yield break;
        }

        var groups = Join(capability.CausingGroups);

        switch (capability.Cause)
        {
            case CapabilityCause.Direct:
                yield return $"Capability control is on and {what} requires {node.CapabilityKey}. {groups} restricts it, so it is hidden.";
                yield return $"Removing that limitation, or removing {snapshot.Subject.DisplayName} from {groups}, restores it.";
                break;

            case CapabilityCause.Cascaded:
                yield return $"{what} requires {capability.CausingKey}, which {groups} restricts.";
                yield return $"There is no limitation on {node.CapabilityKey} itself — it is hidden because a required capability is.";
                break;

            default:
                yield return $"Capability control is on and {what} requires {node.CapabilityKey}. None of the user's groups restricts it, so the capability gate allows it.";
                break;
        }
    }

    private static IEnumerable<string> PermissionSentences(RightsSnapshot snapshot, RightsVerdict verdict, string what)
    {
        var node = verdict.Node;
        var subject = snapshot.Subject;
        var level = node.PermissionLevel ?? Levels.NotSet;

        if (verdict.PermissionGrantsRead)
        {
            yield return node.Origin switch
            {
                PermissionOrigin.Inherited =>
                    $"Section '{node.PermissionKey}' has no row of its own; it inherits {Levels.Name(level)} from section '{node.InheritedFrom}'.",
                PermissionOrigin.RoleDefault when subject.IsAdmin =>
                    $"User type Administrator grants the 'Administrators' role a default of {Levels.Name(level)}, which satisfies section '{node.PermissionKey}'.",
                PermissionOrigin.RoleDefault =>
                    $"Section '{node.PermissionKey}' resolves to {Levels.Name(level)} from a role default.",
                _ =>
                    $"Section '{node.PermissionKey}' grants {Levels.Name(level)} to one of {subject.DisplayName}'s groups."
            };

            if (subject.IsAdmin)
                yield return "Capability limitations still apply to this user — the Administrator user type bypasses permissions only.";

            yield break;
        }

        if (level == Levels.None)
        {
            yield return $"Section '{node.PermissionKey}' is explicitly set to None for one of {subject.DisplayName}'s owners, which denies {what}.";
        }
        else
        {
            yield return $"Section '{node.PermissionKey}' has no permission for any of {subject.DisplayName}'s groups, and 'Authenticated users (backend)' declares no default.";
            yield return "Backend access is grant-only, so the area is hidden.";
        }

        if (node.Kind == RightsNodeKind.Node && node.RequiredLevel is int required && !Levels.GrantsRead(level))
            yield return $"The node additionally requires {Levels.Name(required)} on its parent.";
    }

    private static string Kind(RightsNodeKind kind) => kind switch
    {
        RightsNodeKind.Area => "Area",
        RightsNodeKind.Section => "Section",
        _ => "Node"
    };

    private static string Join(IReadOnlyList<string> names) => names.Count switch
    {
        0 => "A group",
        1 => $"Group '{names[0]}'",
        _ => "Groups " + string.Join(", ", names.Select(n => $"'{n}'"))
    };
}
