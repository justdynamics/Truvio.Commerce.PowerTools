using Truvio.Commerce.PowerTools.Core.Diagnostics;

namespace Truvio.Commerce.PowerTools.Core.Rights.Rules;

/// <summary>The entity names the backend-rights findings carry.</summary>
public static class RightsEntities
{
    public const string Capability = "Capability";
    public const string Section = "PermissionSection";
    public const string BackendUser = "BackendUser";
    public const string Area = "AdminArea";
}

/// <summary>
/// A rule over one <see cref="RightsSnapshot"/>. Deliberately its own contract rather than
/// <c>IWarningRule</c>: that one is built over <c>WarningContext</c>/<c>IContentSecuritySource</c>,
/// which knows about pages and paragraphs and nothing about the admin's own gates. The findings
/// carry the same <see cref="Finding"/> shape, so the Content Access Warnings screen lists them
/// beside the SECOPS-W rules.
/// </summary>
public interface IRightsRule
{
    string RuleId { get; }

    IEnumerable<Finding> Evaluate(RightsSnapshot snapshot);
}

/// <summary>SECOPS-B1 — limitations are stored while the feature that enforces them is off.</summary>
public sealed class InactiveCapabilityLimitationRule : IRightsRule
{
    public const string Id = "SECOPS-B1";

    public string RuleId => Id;

    public IEnumerable<Finding> Evaluate(RightsSnapshot snapshot)
    {
        if (snapshot.CapabilityControlActive || snapshot.Limitations.Count == 0)
            yield break;

        var count = snapshot.Limitations.Count;
        yield return new Finding(
            RuleId,
            FindingSeverity.Info,
            RightsEntities.Capability,
            "capability-control",
            "Capability control",
            $"{count} capability limitation{(count == 1 ? " is" : "s are")} stored but capability control is off",
            "They have no effect today and will take effect the moment the feature is enabled under " +
            "Settings ▸ Administration ▸ Feature Management. Review them before switching it on.");
    }
}

/// <summary>SECOPS-B2 — a limitation on a key no provider declares, e.g. left by an uninstalled app.</summary>
public sealed class UnknownCapabilityKeyRule : IRightsRule
{
    public const string Id = "SECOPS-B2";

    public string RuleId => Id;

    public IEnumerable<Finding> Evaluate(RightsSnapshot snapshot)
    {
        if (!snapshot.CapabilityDataAvailable)
            yield break;

        var declared = snapshot.Capabilities.Select(c => c.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var key in snapshot.Limitations
                     .Where(l => !declared.Contains(l.Key))
                     .Select(l => l.Key)
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            var groups = snapshot.Limitations
                .Where(l => string.Equals(l.Key, key, StringComparison.OrdinalIgnoreCase))
                .Select(l => l.GroupName)
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            yield return new Finding(
                RuleId,
                FindingSeverity.Warning,
                RightsEntities.Capability,
                key,
                key,
                "Limitation references a capability no app declares",
                $"No installed capability provider declares '{key}'{(groups.Count == 0 ? string.Empty : $", yet it is restricted for {string.Join(", ", groups)}")}. " +
                "An unknown key limits nobody, so the restriction silently does nothing — it is usually left behind by an uninstalled app.");
        }
    }
}

/// <summary>SECOPS-B3 — a limitation pointing at a group that no longer exists.</summary>
public sealed class DeletedLimitationGroupRule : IRightsRule
{
    public const string Id = "SECOPS-B3";

    public string RuleId => Id;

    public IEnumerable<Finding> Evaluate(RightsSnapshot snapshot)
    {
        if (!snapshot.CapabilityDataAvailable)
            yield break;

        foreach (var limitation in snapshot.Limitations
                     .Where(l => l.GroupMissing)
                     .OrderBy(l => l.UserGroupId))
        {
            yield return new Finding(
                RuleId,
                FindingSeverity.Warning,
                RightsEntities.Capability,
                $"{limitation.UserGroupId}|{limitation.Key}",
                $"Group {limitation.UserGroupId}",
                "Limitation references a deleted user group",
                $"The limitation on '{limitation.Key}' is owned by user group {limitation.UserGroupId}, which no longer exists. " +
                "The row can never apply again and should be removed.");
        }
    }
}

/// <summary>SECOPS-B4 — a section permission whose key matches no live area (the rename-orphan case).</summary>
public sealed class OrphanedSectionPermissionRule : IRightsRule
{
    public const string Id = "SECOPS-B4";

    public string RuleId => Id;

    public IEnumerable<Finding> Evaluate(RightsSnapshot snapshot)
    {
        foreach (var key in snapshot.OrphanedSectionKeys
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            yield return new Finding(
                RuleId,
                FindingSeverity.Warning,
                RightsEntities.Section,
                key,
                $"Section '{key}'",
                "Permission rows target a section no area declares",
                $"Permissions are stored for section '{key}', but no installed area carries that name. " +
                "A section's permission key is the area's DISPLAY NAME, so renaming an area orphans every row it had — " +
                "the grants stop applying silently and the area falls back to whatever its owners resolve to.");
        }
    }
}

/// <summary>SECOPS-B5 — the user can sign in but every area is hidden.</summary>
public sealed class NoVisibleAreaRule : IRightsRule
{
    public const string Id = "SECOPS-B5";

    public string RuleId => Id;

    public IEnumerable<Finding> Evaluate(RightsSnapshot snapshot)
    {
        if (!snapshot.Subject.AllowBackend || snapshot.Nodes.Count == 0)
            yield break;

        var (visible, total) = RightsEvaluator.AreaCount(RightsEvaluator.Evaluate(snapshot));
        if (total == 0 || visible > 0)
            yield break;

        yield return new Finding(
            RuleId,
            FindingSeverity.Warning,
            RightsEntities.BackendUser,
            snapshot.Subject.UserId.ToString(),
            $"{snapshot.Subject.DisplayName} ({snapshot.Subject.UserId})",
            "Backend access is allowed but no area is visible",
            $"The account can sign in to the administration, but none of the {total} installed areas passes its gates — " +
            "it reaches the admin and sees an empty shell. Grant Read on at least one section, or turn backend access off.");
    }
}

/// <summary>SECOPS-B6 — both gates configured on one area while only one is ever consulted.</summary>
public sealed class DeadGateConfigurationRule : IRightsRule
{
    public const string Id = "SECOPS-B6";

    public string RuleId => Id;

    public IEnumerable<Finding> Evaluate(RightsSnapshot snapshot)
    {
        if (!snapshot.CapabilityControlActive)
            yield break;

        foreach (var node in snapshot.Nodes.Where(n =>
                     n.Kind == RightsNodeKind.Area
                     && n.DeclaresCapability
                     && n.Origin is PermissionOrigin.Explicit or PermissionOrigin.Inherited))
        {
            yield return new Finding(
                RuleId,
                FindingSeverity.Info,
                RightsEntities.Area,
                node.Id,
                node.Name,
                "Section permissions are configured but never consulted",
                $"Area '{node.Name}' declares capability {node.CapabilityKey} and capability control is on, so its section " +
                $"permissions are skipped entirely. The rows on section '{node.PermissionKey}' are dead configuration today — " +
                "they would take effect again if capability control were switched off.");
        }
    }
}

/// <summary>Runs every backend-rights rule over one snapshot, worst finding first.</summary>
public sealed class RightsWarningEngine
{
    private readonly IReadOnlyList<IRightsRule> _rules;

    public RightsWarningEngine() : this(
    [
        new InactiveCapabilityLimitationRule(),
        new UnknownCapabilityKeyRule(),
        new DeletedLimitationGroupRule(),
        new OrphanedSectionPermissionRule(),
        new NoVisibleAreaRule(),
        new DeadGateConfigurationRule()
    ])
    {
    }

    public RightsWarningEngine(IReadOnlyList<IRightsRule> rules) => _rules = rules;

    public IReadOnlyList<Finding> Run(RightsSnapshot snapshot)
    {
        var findings = new List<Finding>();

        foreach (var rule in _rules)
        {
            try
            {
                findings.AddRange(rule.Evaluate(snapshot));
            }
            catch (Exception ex)
            {
                // One unreadable rule must not hide the others.
                findings.Add(new Finding(
                    "SECOPS-BE",
                    FindingSeverity.Info,
                    RightsEntities.Capability,
                    rule.RuleId,
                    rule.GetType().Name,
                    "Rule could not be evaluated",
                    ex.Message));
            }
        }

        return findings
            .OrderBy(f => f.Severity switch
            {
                FindingSeverity.Critical => 0,
                FindingSeverity.Warning => 1,
                _ => 2
            })
            .ThenBy(f => f.RuleId, StringComparer.Ordinal)
            .ThenBy(f => f.EntityDisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
