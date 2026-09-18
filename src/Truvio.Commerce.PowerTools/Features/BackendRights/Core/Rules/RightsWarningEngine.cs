using Truvio.Commerce.PowerTools.Features.BackendRights.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.BackendRights.Core.Rules;

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
