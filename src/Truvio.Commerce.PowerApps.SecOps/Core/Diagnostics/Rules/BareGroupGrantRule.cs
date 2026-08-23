using Truvio.Commerce.PowerApps.SecOps.Core.Permissions;
using Truvio.Commerce.PowerApps.SecOps.Core.Principals;

namespace Truvio.Commerce.PowerApps.SecOps.Core.Diagnostics.Rules;

/// <summary>
/// Detects the classic ineffective gate: a group is granted Read on an entity while the broad
/// frontend roles still resolve to Read (explicitly or via their default). Highest-wins
/// resolution then gives every visitor access regardless of the group grant, so the content
/// is NOT personalised even though the permission panel suggests it is. The working shape is
/// the deny+grant pair: broad role -> None plus group -> Read on the same entity.
/// </summary>
public sealed class BareGroupGrantRule : IWarningRule
{
    public string RuleId => "SECOPS-W1";

    public IEnumerable<Finding> Evaluate(WarningContext context)
    {
        foreach (var entityName in new[] { ContentEntityNames.Page, ContentEntityNames.GridRow, ContentEntityNames.Paragraph })
        {
            foreach (var group in context.Source.GetRows(entityName).GroupBy(r => r.Key))
            {
                var rows = group.ToList();
                var groupGrants = rows
                    .Where(r => int.TryParse(r.OwnerId, out _) && Levels.GrantsRead(r.Level))
                    .ToList();
                if (groupGrants.Count == 0)
                    continue;

                var authenticatedLevel = ExplicitOrDefault(rows, SecurityAccount.AuthenticatedFrontendRole);
                var anonymousLevel = ExplicitOrDefault(rows, SecurityAccount.AnonymousRole);
                var grantedGroups = string.Join(", ", groupGrants.Select(g => g.OwnerId));
                var entity = context.DescribeEntity(entityName, group.Key);

                if (Levels.GrantsRead(authenticatedLevel))
                {
                    yield return new Finding(
                        RuleId,
                        FindingSeverity.Critical,
                        entityName,
                        group.Key,
                        entity,
                        "Group grant has no effect",
                        $"{entity} grants Read to group(s) {grantedGroups}, but every signed-in visitor "
                        + "still resolves to Read through the Authenticated frontend role "
                        + "(highest level wins). Add an explicit 'Authenticated users (frontend) -> None' "
                        + "on this entity to make the group grant gate.");
                }
                else if (Levels.GrantsRead(anonymousLevel))
                {
                    yield return new Finding(
                        RuleId,
                        FindingSeverity.Warning,
                        entityName,
                        group.Key,
                        entity,
                        "Anonymous visitors bypass the group gate",
                        $"{entity} is gated to group(s) {grantedGroups} for signed-in visitors, but "
                        + "anonymous visitors still resolve to Read through the Anonymous role. Add an "
                        + "explicit 'Anonymous users (frontend) -> None' on this entity to close the gap.");
                }
            }
        }
    }

    private static int ExplicitOrDefault(IReadOnlyList<ContentPermissionRow> rows, string role)
    {
        var row = rows.FirstOrDefault(r => string.Equals(r.OwnerId, role, StringComparison.OrdinalIgnoreCase));
        return row?.Level ?? Levels.Read;
    }
}
