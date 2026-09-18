using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.BackendRights.Core.Rules;

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
