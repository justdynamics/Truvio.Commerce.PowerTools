namespace Truvio.Commerce.PowerTools.Core.Operations;

/// <summary>
/// Everything the Operations tools read out of the install. Strictly read-only; the DW-backed
/// implementation lives in <c>Core/Operations/Dw/DwOperationsSource.cs</c>, and the tests use a
/// hand-built fake so the rules never need a DW runtime.
/// </summary>
public interface IOperationsSource
{
    /// <summary>All scheduled tasks.</summary>
    IReadOnlyList<TaskSpec> GetTasks();

    /// <summary>Recorded executions of one task, newest first.</summary>
    IReadOnlyList<TaskRunSpec> GetTaskRuns(int taskId, int max);

    /// <summary>The parameters the task's add-in was saved with, in file order.</summary>
    IReadOnlyList<(string Name, string Value)> GetTaskParameters(int taskId);

    /// <summary>All data-integration activities.</summary>
    IReadOnlyList<ActivitySpec> GetActivities();

    /// <summary>The tail of the newest run log of one activity, newest lines last.</summary>
    IReadOnlyList<string> GetActivityLogTail(string activityId, int maxLines);

    /// <summary>Log folders with their sizes, largest first.</summary>
    IReadOnlyList<StorageFolderSpec> GetLogFolders();

    /// <summary>Database tables with row counts and reserved size, largest first.</summary>
    IReadOnlyList<TableSizeSpec> GetTableSizes();

    /// <summary>The install's log-retention configuration.</summary>
    RetentionSpec GetRetention();

    /// <summary>Recent changes with attribution where the install keeps it, newest first.</summary>
    IReadOnlyList<ChangeSpec> GetRecentChanges(int days, int max);
}
