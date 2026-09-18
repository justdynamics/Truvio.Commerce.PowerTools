using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.BackendRights.Core.Rules;

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
