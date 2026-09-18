using Truvio.Commerce.PowerTools.Features.OperationsConsole.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Core.Rules;

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

    private readonly long _warningBytes;
    private readonly long _criticalBytes;

    public LogGrowthRule() : this(WarningBytes, CriticalBytes)
    {
    }

    /// <summary>Configurable through PowerTools settings; non-positive sizes fall back to the defaults.</summary>
    public LogGrowthRule(long warningBytes, long criticalBytes)
    {
        _warningBytes = warningBytes > 0 ? warningBytes : WarningBytes;
        _criticalBytes = criticalBytes > 0 ? criticalBytes : CriticalBytes;
    }

    public IEnumerable<Finding> Evaluate(OperationsSnapshot snapshot)
    {
        foreach (var folder in snapshot.LogFolders.Where(f => f.Bytes >= _warningBytes))
        {
            yield return new Finding(
                SizeId,
                folder.Bytes >= _criticalBytes ? FindingSeverity.Critical : FindingSeverity.Warning,
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
