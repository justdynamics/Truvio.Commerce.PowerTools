using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.BackendRights.Core.Rules;

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
