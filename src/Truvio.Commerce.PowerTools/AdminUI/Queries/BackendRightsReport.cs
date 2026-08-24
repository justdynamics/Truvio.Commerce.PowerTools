using System.Text.Json;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Diagnostics;
using Truvio.Commerce.PowerTools.Core.Permissions;
using Truvio.Commerce.PowerTools.Core.Rights;
using Truvio.Commerce.PowerTools.Core.Rights.Rules;
using Truvio.Commerce.PowerTools.Core.Settings.Dw;
using static Truvio.Commerce.PowerTools.AdminUI.SearchTables;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// Renders the backend-rights report. The tables are HTML inside an overview screen, not a list
/// grid: the grid gives every column the same width and never wraps, and these explanations are
/// long text.
/// </summary>
internal static class BackendRightsReport
{
    public static BackendRightsModel Build(RightsSnapshot snapshot, bool showTree, string accountKey)
    {
        var verdicts = RightsEvaluator.Evaluate(snapshot);
        var (visibleAreas, totalAreas) = RightsEvaluator.AreaCount(verdicts);
        var subject = snapshot.Subject;

        var model = new BackendRightsModel
        {
            Title = $"{subject.DisplayName} (id {subject.UserId})",
            UserName = subject.UserName,
            BackendAccess = subject.AllowBackend,
            Status = subject.StatusName,
            AreasVisible = $"{visibleAreas} of {totalAreas}",
            GateInForce = snapshot.CapabilityControlActive
                ? "Capabilities (feature on)"
                : snapshot.CapabilityDataAvailable ? "Permissions" : "Permissions (capability control unavailable)"
        };

        model.Sections.Add(AreasSection(snapshot, verdicts, showTree, accountKey));

        if (LimitationsSection(snapshot) is { } limitations)
            model.Sections.Add(limitations);

        model.Sections.Add(OwnersSection(snapshot));

        if (FindingsSection(snapshot) is { } findings)
            model.Sections.Add(findings);

        return model;
    }

    // ---- Section 1: the tree --------------------------------------------------------------------

    private static ReportSectionModel AreasSection(
        RightsSnapshot snapshot, IReadOnlyList<RightsVerdict> verdicts, bool showTree, string accountKey)
    {
        var rows = new List<IReadOnlyList<object?>>();

        foreach (var verdict in verdicts)
        {
            var node = verdict.Node;
            if (!showTree && node.Kind != RightsNodeKind.Area)
                continue;

            var indent = node.Kind switch
            {
                RightsNodeKind.Area => string.Empty,
                RightsNodeKind.Section => "— ",
                _ => "— — "
            };

            rows.Add([
                new Wrap($"{indent}{node.Name}"),
                new Pill(verdict.Visible ? "Yes" : "No", verdict.Visible ? "ok" : "bad"),
                DecidedBy(verdict),
                new Wrap(CapabilityCell(snapshot, verdict)),
                new Wrap(PermissionCell(snapshot, verdict)),
                new Wrap(string.IsNullOrEmpty(node.LicenseFeature) ? "—" : $"{node.LicenseFeature}: {(node.LicenseOk ? "ok" : "missing")}"),
                WhyLink(accountKey, node.Id)
            ]);
        }

        var html = Table(["Area", "Sees it", "Decided by", "Capability", "Permission", "License", ""], rows);

        html += Note(snapshot.CapabilityControlActive
            ? "Capability control is ON. Where an area or node declares a capability, its section permissions are not consulted at all — the greyed permission is stored but dead today."
            : "Capability control is OFF, so section permissions decide everywhere and any capability a component declares is ignored.");

        if (showTree)
        {
            html += Note("Sections and nodes are indented under their area. A section's permission level is its AREA's — " +
                         "Dynamicweb never stores a permission per section — and with capability control on, a section that declares " +
                         "no capability is not permission-checked at composition time either.");
        }

        return new ReportSectionModel { Heading = "Admin areas", Html = html };
    }

    private static Pill DecidedBy(RightsVerdict verdict) => verdict.DecidedBy switch
    {
        RightsGate.Capability => new Pill("Capability", verdict.Visible ? "info" : "bad"),
        RightsGate.Permission => new Pill("Permission", verdict.Visible ? "info" : "bad"),
        RightsGate.License => new Pill("License", "warn"),
        RightsGate.Bypass => new Pill("Bypass", "warn"),
        _ => new Pill("Not gated", "")
    };

    private static string CapabilityCell(RightsSnapshot snapshot, RightsVerdict verdict)
    {
        var node = verdict.Node;
        if (!node.DeclaresCapability)
            return "— none declared";

        if (!snapshot.CapabilityDataAvailable)
            return $"{node.CapabilityKey} (data unavailable)";

        var state = verdict.Capability.Cause switch
        {
            CapabilityCause.Direct => $"Restricted by {string.Join(", ", verdict.Capability.CausingGroups)}",
            CapabilityCause.Cascaded => $"Restricted via {verdict.Capability.CausingKey}",
            _ => "Allowed"
        };

        return snapshot.CapabilityControlActive
            ? $"{node.CapabilityKey} — {state}"
            : $"{node.CapabilityKey} — {state} (not enforced)";
    }

    private static string PermissionCell(RightsSnapshot snapshot, RightsVerdict verdict)
    {
        var node = verdict.Node;
        if (node.PermissionLevel is not int level)
            return "—";

        var origin = node.Origin switch
        {
            PermissionOrigin.Explicit => "explicit",
            PermissionOrigin.Inherited => $"inherited from '{node.InheritedFrom}'",
            PermissionOrigin.RoleDefault => "role default",
            PermissionOrigin.ContextDefault => "no grant",
            _ => "not evaluated"
        };

        var text = $"{Levels.Name(level)} ({origin})";
        return verdict.PermissionConsulted ? text : $"{text} — not consulted";
    }

    // ---- Section 2: capability limitations --------------------------------------------------------

    private static ReportSectionModel? LimitationsSection(RightsSnapshot snapshot)
    {
        if (!snapshot.CapabilityDataAvailable)
        {
            return new ReportSectionModel
            {
                Heading = "Capability limitations",
                Html = Note("Capability control is not available on this host (the feature ships with Dynamicweb 10.19 and newer), " +
                            "so every verdict above comes from section permissions.")
            };
        }

        var mine = snapshot.Limitations
            .Where(l => snapshot.UserGroupIds.Contains(l.UserGroupId))
            .ToList();

        if (snapshot.Limitations.Count == 0)
            return null;

        var rows = new List<IReadOnlyList<object?>>();
        foreach (var capability in snapshot.Capabilities.OrderBy(c => c.Key, StringComparer.OrdinalIgnoreCase))
        {
            var verdict = RightsEvaluator.ResolveCapability(snapshot, capability.Key);
            if (!verdict.Restricted)
                continue;

            rows.Add([
                new Wrap(capability.Key),
                new Wrap(capability.Name),
                new Pill(verdict.Cause == CapabilityCause.Direct ? "Direct" : "Cascaded",
                    verdict.Cause == CapabilityCause.Direct ? "bad" : "warn"),
                new Wrap(verdict.Cause == CapabilityCause.Cascaded
                    ? $"via {verdict.CausingKey} — {string.Join(", ", verdict.CausingGroups)}"
                    : string.Join(", ", verdict.CausingGroups))
            ]);
        }

        var heading = snapshot.CapabilityControlActive ? "Capability limitations" : "Capability limitations (stored but inactive)";

        if (rows.Count == 0)
        {
            return new ReportSectionModel
            {
                Heading = heading,
                Html = Note(mine.Count == 0
                    ? $"{snapshot.Limitations.Count} limitation(s) exist on this solution, but none applies to this user's groups."
                    : "No capability is restricted for this user.")
            };
        }

        var html = Table(["Capability", "Name", "Cause", "Restricted by"], rows);
        html += Note(snapshot.CapabilityControlActive
            ? "A limitation is a DENY and is stored per user GROUP — one group carrying the row is enough to restrict everyone in it. " +
              "A cascaded row has no limitation of its own: a capability it REQUIRES is restricted."
            : "These rows are stored but capability control is off, so they restrict nobody today. They take effect the moment the feature is enabled.");

        return new ReportSectionModel { Heading = heading, Html = html };
    }

    // ---- Section 3: owner priority ------------------------------------------------------------------

    private static ReportSectionModel OwnersSection(RightsSnapshot snapshot)
    {
        var rows = new List<IReadOnlyList<object?>>();

        foreach (var level in snapshot.OwnerLevels)
        {
            foreach (var owner in level.Owners)
            {
                rows.Add([
                    new Wrap(level.Level.ToString()),
                    new Wrap(level.Description),
                    new Wrap(owner.DisplayName),
                    new Wrap(owner.Kind),
                    new Wrap(owner.DefaultLevel is int d ? Levels.Name(d) : "none")
                ]);
            }
        }

        var html = Table(["Priority", "Level", "Owner", "Kind", "Default"], rows);
        html += Note("Permissions resolve level by level in this order and stop at the FIRST level that produces any value; " +
                     "inside one level the owners are merged most-permissive-wins. The user's own id is never an owner in the backend, " +
                     "and 'Authenticated users (backend)' declares no default — which is why backend access is grant-only.");

        return new ReportSectionModel { Heading = "Group membership & priority", Html = html };
    }

    // ---- Section 4: findings --------------------------------------------------------------------------

    private static ReportSectionModel? FindingsSection(RightsSnapshot snapshot)
    {
        var findings = new RightsWarningEngine().Run(snapshot);
        var filtered = DwPowerToolsSettings.Current.FilterWarningFindings(findings);
        if (filtered.Visible.Count == 0 && filtered.HiddenCount == 0)
            return null;

        var showRuleIds = DwPowerToolsSettings.Current.ShowRuleIds;
        var rows = filtered.Visible.Select(f => (IReadOnlyList<object?>)
        [
            new Pill(f.Severity.ToString(), f.Severity switch
            {
                FindingSeverity.Critical => "bad",
                FindingSeverity.Warning => "warn",
                _ => "info"
            }),
            showRuleIds ? new Wrap(f.RuleId) : new Wrap(string.Empty),
            new Wrap(f.EntityDisplayName),
            new Wrap($"{f.Title} — {f.Detail}")
        ]).ToList();

        var html = Table(["", showRuleIds ? "Rule" : "", "Subject", "Finding"], rows);
        if (filtered.HiddenCount > 0)
            html += Note($"{filtered.HiddenNotice()} — suppressed rules are configured under PowerTools ▸ Settings.");

        return new ReportSectionModel { Heading = "Findings", Html = html };
    }

    // ---- The Why? panel ---------------------------------------------------------------------------------

    public static string WhyHtml(RightsSnapshot snapshot, RightsVerdict verdict)
    {
        var html = string.Empty;

        foreach (var sentence in RightsExplanation.Sentences(snapshot, verdict))
            html += Note(sentence);

        var node = verdict.Node;
        var facts = new List<IReadOnlyList<object?>>();
        facts.Add(new object?[] { new Wrap("Sees it"), new Pill(verdict.Visible ? "Yes" : "No", verdict.Visible ? "ok" : "bad") });
        facts.Add(new object?[] { new Wrap("Decided by"), DecidedBy(verdict) });
        facts.Add(new object?[] { new Wrap("Capability"), new Wrap(CapabilityCell(snapshot, verdict)) });
        facts.Add(new object?[] { new Wrap("Permission"), new Wrap(PermissionCell(snapshot, verdict)) });

        if (!string.IsNullOrEmpty(node.LicenseFeature))
            facts.Add(new object?[] { new Wrap("License"), new Wrap($"{node.LicenseFeature}: {(node.LicenseOk ? "ok" : "missing")}") });

        // Stacked, two columns — the slide-over panel is narrow.
        return html + Table([], facts);
    }

    /// <summary>
    /// A link that opens the panel as a slide-over. The client's delegated handler picks up
    /// <c>data-dw-action</c> on any element, including raw HTML inside an HtmlBlock; the href stays
    /// as the full-navigation fallback.
    /// </summary>
    private static ActionLink WhyLink(string accountKey, string nodeId)
    {
        var action = new Dictionary<string, object?>
        {
            ["name"] = "OpenSlideOver",
            ["parameters"] = new Dictionary<string, object?>
            {
                ["ScreenTypeName"] = "BackendRightsWhy",
                ["ScreenType"] = "slideOver",
                ["Query"] = new Dictionary<string, object?>
                {
                    ["AccountKey"] = accountKey,
                    ["NodeId"] = nodeId,
                    ["Type"] = "BackendRightsWhy",
                    ["QueryContext"] = new Dictionary<string, object?> { ["screenTypeName"] = "BackendRightsWhy" }
                },
                ["ForceReload"] = false,
                ["NavigateByPost"] = false
            }
        };

        var href = "/Admin/UI/PowerTools/BackendRightsWhy" +
                   $"?AccountKey={Uri.EscapeDataString(accountKey)}&NodeId={Uri.EscapeDataString(nodeId)}" +
                   "&Type=BackendRightsWhy&QueryContext=Dynamicweb.CoreUI.Data.DataQueryContext";

        return new ActionLink("Why?", href, JsonSerializer.Serialize(action));
    }
}
