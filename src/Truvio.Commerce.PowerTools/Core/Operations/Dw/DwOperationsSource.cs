using Truvio.Commerce.PowerTools.Core.Commerce;
using Truvio.Commerce.PowerTools.Core.Commerce.Dw;
using Truvio.Commerce.PowerTools.Core.Settings.Dw;

namespace Truvio.Commerce.PowerTools.Core.Operations.Dw;

/// <summary>
/// <see cref="IOperationsSource"/> backed by the live DW runtime. Strictly read-only: task and
/// audit data through DW's own services, activity definitions and log sizes off disk, table
/// sizes through a SELECT against SQL Server's own catalogue views. Nothing here writes,
/// deletes, or executes an add-in.
/// </summary>
public sealed class DwOperationsSource : IOperationsSource
{
    /// <summary>How many tables the storage screen lists.</summary>
    public const int TableCount = 15;

    public IReadOnlyList<TaskSpec> GetTasks() => DwTaskReader.GetTasks();

    public IReadOnlyList<TaskRunSpec> GetTaskRuns(int taskId, int max) => DwTaskReader.GetRuns(taskId, max);

    public IReadOnlyList<(string Name, string Value)> GetTaskParameters(int taskId) => DwTaskReader.GetParameters(taskId);

    public IReadOnlyList<ActivitySpec> GetActivities() => DwActivityReader.GetActivities();

    public IReadOnlyList<string> GetActivityLogTail(string activityId, int maxLines) =>
        DwActivityReader.GetLogTail(activityId, maxLines);

    public IReadOnlyList<StorageFolderSpec> GetLogFolders() => DwStorageReader.GetLogFolders();

    public IReadOnlyList<TableSizeSpec> GetTableSizes() => DwStorageReader.GetTableSizes(TableCount);

    public RetentionSpec GetRetention() => DwStorageReader.GetRetention();

    public IReadOnlyList<ChangeSpec> GetRecentChanges(int days, int max) => DwChangeReader.GetRecentChanges(days, max);

    /// <summary>
    /// One read of everything the rules need. Tasks and activities are always read; storage is
    /// the expensive part, so screens that only care about tasks can skip it.
    /// </summary>
    public OperationsSnapshot Snapshot(bool includeStorage = true)
    {
        var tasks = GetTasks();
        var activities = GetActivities();
        var folders = includeStorage ? GetLogFolders() : [];
        var tables = includeStorage ? GetTableSizes() : [];
        var databaseBytes = includeStorage ? DwStorageReader.GetDatabaseBytes() : 0;
        var settings = DwPowerToolsSettings.Current;

        return new OperationsSnapshot(
            tasks,
            activities,
            folders,
            tables,
            GetRetention(),
            DateTime.Now,
            databaseBytes)
        {
            Currencies = DwCurrencyReader.GetCurrencies(),
            PriceSamples = includeStorage ? DwCurrencyReader.GetPriceSamples() : [],
            // The one optional outbound call in the suite; a failed fetch means "no live
            // comparison", never a finding.
            LiveEurRates = settings.LiveRateCheckEnabled ? EcbRateSource.Fetch(settings.LiveRateFeedUrl) : null
        };
    }
}
