using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.BackendRights.Core.Rules;

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
