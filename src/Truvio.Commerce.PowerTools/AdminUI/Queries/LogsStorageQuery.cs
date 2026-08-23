using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Diagnostics;
using Truvio.Commerce.PowerTools.Core.Operations;
using Truvio.Commerce.PowerTools.Core.Operations.Dw;
using Truvio.Commerce.PowerTools.Core.Operations.Rules;
using Truvio.Commerce.PowerTools.Core.Settings.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// What is growing: the log folders on disk, the biggest tables in the database, the retention
/// configuration that decides whether either ever shrinks, and the findings those three imply.
/// </summary>
public sealed class LogsStorageQuery : DataQueryModelBase<LogsStorageModel>
{
    /// <summary>How many log folders the screen lists before it stops.</summary>
    public const int FolderCount = 20;

    public override LogsStorageModel? GetModel()
    {
        try
        {
            var settings = DwPowerToolsSettings.Current;
            var source = new DwOperationsSource();
            var snapshot = source.Snapshot();
            var findings = new OperationsHealthEngine(
                [OperationsHealthEngine.LogGrowth(settings), OperationsHealthEngine.TableBloat(settings)]).Run(snapshot);

            var model = new LogsStorageModel
            {
                LogTotal = OpsFormat.Bytes(snapshot.TotalLogBytes),
                DatabaseTotal = snapshot.TotalTableBytes > 0 ? OpsFormat.Bytes(snapshot.TotalTableBytes) : "unavailable",
                RetentionEnabled = snapshot.Retention.PurgeEnabled,
                RetentionSummary = snapshot.Retention.PurgeEnabled ? "On" : "Off",
                FindingCount = findings.Count
            };

            foreach (var folder in snapshot.LogFolders.Take(FolderCount))
            {
                model.Folders.Add(new OpsRowModel
                {
                    Item = folder.Name,
                    Verdict = folder.Bytes >= LogGrowthRule.CriticalBytes ? "Huge"
                            : folder.Bytes >= LogGrowthRule.WarningBytes ? "Large"
                            : string.Empty,
                    VerdictKind = folder.Bytes >= LogGrowthRule.CriticalBytes ? "reject"
                                : folder.Bytes >= LogGrowthRule.WarningBytes ? "warn"
                                : string.Empty,
                    Value = OpsFormat.Bytes(folder.Bytes),
                    Why = $"{folder.FileCount} file(s), {OpsFormat.Absolute(folder.Oldest)} → {OpsFormat.Absolute(folder.Newest)}" +
                          (folder.SpanDays >= 1 ? $" ({(int)folder.SpanDays} days of history)" : string.Empty)
                });
            }

            if (model.Folders.Count == 0)
                model.Folders.Add(new OpsRowModel { Item = "Log folders", Verdict = "none", Why = "No log files were found under /Files/System/Log." });

            var total = snapshot.TotalTableBytes;
            foreach (var table in snapshot.Tables)
            {
                var share = total > 0 ? (double)table.Bytes / total : 0;
                model.Tables.Add(new OpsRowModel
                {
                    Item = table.Name,
                    Verdict = share >= TableBloatRule.ShareThreshold && table.Bytes >= TableBloatRule.ShareFloorBytes
                        ? $"{share:P0}"
                        : string.Empty,
                    VerdictKind = table.Bytes >= TableBloatRule.CriticalBytes ? "reject" : "warn",
                    Value = OpsFormat.Bytes(table.Bytes),
                    Why = $"{table.RowCount:N0} row(s)" +
                          (TableBloatRule.IsKnownGrowthTable(table.Name)
                              ? snapshot.Retention.CoversTable(table.Name)
                                  ? " — append-only log table, covered by database retention"
                                  : " — append-only log table, no retention configured"
                              : string.Empty)
                });
            }

            if (model.Tables.Count == 0)
            {
                model.Tables.Add(new OpsRowModel
                {
                    Item = "Table sizes",
                    Verdict = "unavailable",
                    VerdictKind = "info",
                    Why = "Reading sys.dm_db_partition_stats failed — the database login needs VIEW DATABASE STATE."
                });
            }

            model.Retention.AddRange(BuildRetention(snapshot.Retention));
            model.Findings.AddRange(findings.Select(ToRow));
            if (findings.Count == 0)
                model.Findings.Add(new OpsRowModel { Item = "Findings", Verdict = "none", VerdictKind = "ok", Why = "Nothing on this install looks like runaway growth." });

            return model;
        }
        catch (Exception ex)
        {
            return new LogsStorageModel { Error = ex.Message };
        }
    }

    private static IEnumerable<OpsRowModel> BuildRetention(RetentionSpec retention)
    {
        yield return new OpsRowModel
        {
            Item = "Log purging",
            Verdict = retention.PurgeEnabled ? "On" : "Off",
            VerdictKind = retention.PurgeEnabled ? "ok" : "warn",
            Why = retention.PurgeEnabled
                ? "The \"Cleanup logs\" scheduled task trims the locations and tables below."
                : "Nothing is ever trimmed: log files and log tables grow until the disk or the database does."
        };

        yield return new OpsRowModel
        {
            Item = "File locations",
            Value = retention.FileLocations.Count == 0 ? "none" : string.Join(", ", retention.FileLocations),
            Why = "Folders under /Files the cleanup task purges."
        };

        yield return new OpsRowModel
        {
            Item = "Database tables",
            Verdict = retention.DbTables.Count == 0 ? "none" : string.Empty,
            VerdictKind = retention.DbTables.Count == 0 ? "warn" : string.Empty,
            Value = retention.DbTables.Count == 0 ? "none configured" : string.Join(", ", retention.DbTables),
            Why = retention.DbTables.Count == 0
                ? "No log table is trimmed, however large it gets."
                : "Tables the cleanup task deletes old rows from."
        };
    }

    internal static OpsRowModel ToRow(Finding finding) => new()
    {
        Item = finding.EntityDisplayName,
        Verdict = finding.Severity.ToString(),
        VerdictKind = finding.Severity switch
        {
            FindingSeverity.Critical => "reject",
            FindingSeverity.Warning => "warn",
            _ => "info"
        },
        Value = finding.Title,
        Why = $"{finding.Detail} [{finding.RuleId}]"
    };
}
