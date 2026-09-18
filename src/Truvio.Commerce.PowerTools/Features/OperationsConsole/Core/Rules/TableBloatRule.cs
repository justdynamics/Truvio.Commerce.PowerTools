using Truvio.Commerce.PowerTools.Features.OperationsConsole.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Core.Rules;

/// <summary>
/// OPS-W8/W9 — database bloat.
/// <list type="bullet">
/// <item>OPS-W8: one table holding a quarter or more of the whole database. That is what a
/// runaway log table looks like from the outside — the database grows, and one table is
/// nearly all of it.</item>
/// <item>OPS-W9: a known append-only growth table with a lot of rows that DW's database
/// retention settings do not cover, so nothing ever trims it.</item>
/// </list>
/// A floor of 10 MB (share) / 100 000 rows (retention) keeps a small development database
/// from producing noise.
/// </summary>
public sealed class TableBloatRule : IOperationsRule
{
    public const string ShareId = "OPS-W8";
    public const string RetentionId = "OPS-W9";

    public const double ShareThreshold = 0.25;
    public const long ShareFloorBytes = 10L * 1024 * 1024;
    public const long CriticalBytes = 1024L * 1024 * 1024;
    public const long RetentionRowFloor = 100_000;

    private readonly double _shareThreshold;

    public TableBloatRule() : this(ShareThreshold)
    {
    }

    /// <summary>Configurable through PowerTools settings (0 &lt; share ≤ 1); anything else falls back.</summary>
    public TableBloatRule(double shareThreshold) =>
        _shareThreshold = shareThreshold is > 0 and <= 1 ? shareThreshold : ShareThreshold;

    /// <summary>
    /// Tables DW only ever appends to. Trimming them is retention-configuration work, never a
    /// schema change, so naming them is safe and stable.
    /// </summary>
    public static readonly IReadOnlyList<string> KnownGrowthTables =
    [
        "CommandLog",
        "CommandLogModelData",
        "CommandLogModelDataChange",
        "CommandLogModelRelation",
        "GeneralLog",
        "EventLog",
        "Audit",
        "AuditDetail",
        "AuditDeletedObject",
        "ScheduledTaskExecution",
        "Notification",
        "NotificationSubscriber",
        "EcomOrderDebuggingInfo",
        "StatV2Session",
        "StatV2Request"
    ];

    public IEnumerable<Finding> Evaluate(OperationsSnapshot snapshot)
    {
        var total = snapshot.TotalTableBytes;

        if (total > 0)
        {
            foreach (var table in snapshot.Tables)
            {
                var share = (double)table.Bytes / total;
                if (share < _shareThreshold || table.Bytes < ShareFloorBytes)
                    continue;

                yield return new Finding(
                    ShareId,
                    table.Bytes >= CriticalBytes ? FindingSeverity.Critical : FindingSeverity.Warning,
                    OperationsEntities.DatabaseTable,
                    table.Name,
                    table.Name,
                    $"{share:P0} of the database — {OpsFormat.Bytes(table.Bytes)}",
                    $"{table.RowCount:N0} row(s) out of {OpsFormat.Bytes(total)} total. " +
                    (IsKnownGrowthTable(table.Name)
                        ? "This is an append-only log table; trim it through the database retention settings."
                        : "Check whether the data is still needed before the database outgrows its storage."));
            }
        }

        foreach (var table in snapshot.Tables)
        {
            if (table.RowCount < RetentionRowFloor || !IsKnownGrowthTable(table.Name))
                continue;
            if (snapshot.Retention.PurgeEnabled && snapshot.Retention.CoversTable(table.Name))
                continue;

            yield return new Finding(
                RetentionId,
                FindingSeverity.Warning,
                OperationsEntities.DatabaseTable,
                table.Name,
                table.Name,
                $"{table.RowCount:N0} rows and no retention configured",
                snapshot.Retention.PurgeEnabled
                    ? $"Log purging is on but '{table.Name}' is not in the database retention table list, so it is never trimmed."
                    : "Log purging is off for this install, so no log table is ever trimmed.");
        }
    }

    public static bool IsKnownGrowthTable(string name) =>
        KnownGrowthTables.Any(t => string.Equals(t, name, StringComparison.OrdinalIgnoreCase));
}
