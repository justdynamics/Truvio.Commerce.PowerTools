using Truvio.Commerce.PowerTools.Features.BackendRights.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.BackendRights.Core.Rules;

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
