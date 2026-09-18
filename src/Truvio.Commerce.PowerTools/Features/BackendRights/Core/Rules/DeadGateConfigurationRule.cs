using Truvio.Commerce.PowerTools.Features.BackendRights.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.BackendRights.Core.Rules;

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
