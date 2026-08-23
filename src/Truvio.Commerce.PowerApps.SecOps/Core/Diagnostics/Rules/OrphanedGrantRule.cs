using Truvio.Commerce.PowerApps.SecOps.Core.Permissions;

namespace Truvio.Commerce.PowerApps.SecOps.Core.Diagnostics.Rules;

/// <summary>
/// Permission rows whose owner is a numeric group id that no longer exists. They contribute
/// nothing at resolution time but clutter the model and often indicate a deleted group whose
/// gating intent silently evaporated.
/// </summary>
public sealed class OrphanedGrantRule : IWarningRule
{
    public string RuleId => "SECOPS-W4";

    public IEnumerable<Finding> Evaluate(WarningContext context)
    {
        var existingGroups = context.Source.GetExistingGroupIds();

        foreach (var entityName in new[] { ContentEntityNames.Page, ContentEntityNames.GridRow, ContentEntityNames.Paragraph })
        {
            foreach (var row in context.Source.GetRows(entityName))
            {
                if (!int.TryParse(row.OwnerId, out _) || existingGroups.Contains(row.OwnerId))
                    continue;

                var entity = context.DescribeEntity(entityName, row.Key);
                yield return new Finding(
                    RuleId,
                    FindingSeverity.Info,
                    entityName,
                    row.Key,
                    entity,
                    "Permission row references a deleted group",
                    $"{entity} carries a '{Levels.Name(row.Level)}' row for group id {row.OwnerId}, "
                    + "which no longer exists. The row has no effect; remove it, or recreate the "
                    + "group if the gating intent still applies.");
            }
        }
    }
}
