using Truvio.Commerce.PowerTools.Core.Diagnostics;

namespace Truvio.Commerce.PowerTools.Core.Operations.Rules;

/// <summary>
/// OPS-W6/W7 — log folders on disk.
/// <list type="bullet">
/// <item>OPS-W6: a single log folder over 2 GB is Critical, over 500 MB a Warning. Log volume
/// is the one thing that grows without anybody choosing it.</item>
/// <item>OPS-W7: a folder that keeps more than 30 days of files while DW's log purge is off.
/// The purge is the only thing that rotates DW's file logs, and it is off by default.</item>
/// </list>
/// </summary>
public sealed class LogGrowthRule : IOperationsRule
{
    public const string SizeId = "OPS-W6";
    public const string RetentionId = "OPS-W7";

    public const long WarningBytes = 500L * 1024 * 1024;
    public const long CriticalBytes = 2L * 1024 * 1024 * 1024;
    public const double UnrotatedDays = 30;

    public IEnumerable<Finding> Evaluate(OperationsSnapshot snapshot)
    {
        foreach (var folder in snapshot.LogFolders.Where(f => f.Bytes >= WarningBytes))
        {
            yield return new Finding(
                SizeId,
                folder.Bytes >= CriticalBytes ? FindingSeverity.Critical : FindingSeverity.Warning,
                OperationsEntities.LogFolder,
                folder.RelativePath,
                folder.Name,
                $"Log folder is {OpsFormat.Bytes(folder.Bytes)}",
                $"{folder.FileCount} file(s), oldest {OpsFormat.Absolute(folder.Oldest)}, newest {OpsFormat.Absolute(folder.Newest)}. " +
                (snapshot.Retention.PurgeEnabled
                    ? "Log purging is on — check this folder is one of the configured locations."
                    : "Log purging is off, so nothing will ever shrink this folder."));
        }

        if (snapshot.Retention.PurgeEnabled)
            yield break;

        foreach (var folder in snapshot.LogFolders.Where(f => f.SpanDays > UnrotatedDays && f.FileCount > 1))
        {
            yield return new Finding(
                RetentionId,
                FindingSeverity.Warning,
                OperationsEntities.LogFolder,
                folder.RelativePath,
                folder.Name,
                $"Keeps {(int)folder.SpanDays} days of logs with purging disabled",
                $"{OpsFormat.Bytes(folder.Bytes)} across {folder.FileCount} file(s) from {OpsFormat.Absolute(folder.Oldest)} onwards. " +
                "Log retention is switched off for this install (Settings ▸ Logging), so files are never removed.");
        }
    }
}

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
                if (share < ShareThreshold || table.Bytes < ShareFloorBytes)
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
