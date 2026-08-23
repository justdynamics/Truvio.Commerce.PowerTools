namespace Truvio.Commerce.PowerTools.Core.Operations;

/// <summary>
/// One scheduled task, flattened from <c>Dynamicweb.Scheduling.Task</c> so the rules can be
/// unit-tested without a DW runtime.
/// </summary>
/// <param name="Id">Task id (<c>ScheduledTask.TaskId</c>).</param>
/// <param name="Name">Task name.</param>
/// <param name="AddInTypeName">Assembly-qualified add-in type, e.g. <c>Ns.MyAddIn, MyAssembly</c>.</param>
/// <param name="Enabled">Whether the task is enabled.</param>
/// <param name="IntervalMinutes">Repeat interval in minutes; 0 or less means "no repeat".</param>
/// <param name="ScheduleSummary">DW's own human-readable schedule description (<c>Task.Schedule</c>).</param>
/// <param name="LastRun">Last execution time, null when never run.</param>
/// <param name="NextRun">Next planned execution, null when none is planned.</param>
/// <param name="LastResult">DW's stored last result flag; null when unknown.</param>
/// <param name="LastException">DW's stored last exception text; empty when none.</param>
/// <param name="LinkedActivityId">Data-integration activity id this task runs, or empty.</param>
/// <param name="Comment">Free-text comment stored on the task.</param>
public sealed record TaskSpec(
    int Id,
    string Name,
    string AddInTypeName,
    bool Enabled,
    int IntervalMinutes,
    string ScheduleSummary,
    DateTime? LastRun,
    DateTime? NextRun,
    bool? LastResult,
    string LastException,
    string LinkedActivityId,
    string Comment)
{
    /// <summary>The add-in class name without namespace or assembly, for narrow list columns.</summary>
    public string AddInShortName => OpsFormat.ShortTypeName(AddInTypeName);

    /// <summary>True when DW recorded a failure for the most recent run.</summary>
    public bool LastRunFailed => LastRun is not null && (LastResult == false || !string.IsNullOrWhiteSpace(LastException));
}

/// <summary>One recorded execution of a scheduled task.</summary>
/// <param name="TaskId">Owning task id.</param>
/// <param name="ScheduleTime">When the run was scheduled for.</param>
/// <param name="StartTime">When it actually started.</param>
/// <param name="EndTime">When it ended; null when it never finished.</param>
/// <param name="TriggeredBy">Who triggered it — a user name, or "Scheduler" when unattended.</param>
/// <param name="Succeeded">Result flag; null when unknown.</param>
/// <param name="Message">Output/exception text DW stored for the run.</param>
public sealed record TaskRunSpec(
    int TaskId,
    DateTime? ScheduleTime,
    DateTime? StartTime,
    DateTime? EndTime,
    string TriggeredBy,
    bool? Succeeded,
    string Message)
{
    public TimeSpan? Duration => StartTime is { } s && EndTime is { } e && e >= s ? e - s : null;
}

/// <summary>A data-integration activity (a job XML file under the integration jobs folder).</summary>
/// <param name="Id">Activity identifier — <c>group\name</c>, or just <c>name</c> at the root.</param>
/// <param name="Name">Activity name.</param>
/// <param name="Group">Group folder, empty for root-level activities.</param>
/// <param name="Description">Description stored in the job XML.</param>
/// <param name="SourceProvider">Source provider type name.</param>
/// <param name="DestinationProvider">Destination provider type name.</param>
/// <param name="TableCount">Number of source tables in the job schema.</param>
/// <param name="MappingCount">Number of table mappings.</param>
/// <param name="ColumnMappingCount">Number of column mappings across all table mappings.</param>
/// <param name="LastRun">Last run time from DW's <c>_lastrun.log</c> marker (or the newest run log).</param>
/// <param name="LastResult">DW's <c>JobResult</c> name from the <c>_lastrunresult.log</c> marker.</param>
/// <param name="LastDuration">Elapsed time of the last run, when derivable.</param>
/// <param name="ModifiedAt">Last write time of the job XML file.</param>
public sealed record ActivitySpec(
    string Id,
    string Name,
    string Group,
    string Description,
    string SourceProvider,
    string DestinationProvider,
    int TableCount,
    int MappingCount,
    int ColumnMappingCount,
    DateTime? LastRun,
    string LastResult,
    TimeSpan? LastDuration,
    DateTime? ModifiedAt)
{
    public string SourceShortName => OpsFormat.ShortTypeName(SourceProvider);

    public string DestinationShortName => OpsFormat.ShortTypeName(DestinationProvider);
}

/// <summary>A log folder (or other file store) with its aggregate size and age span.</summary>
/// <param name="Name">Display name, e.g. "System/Log/ScheduledTasks".</param>
/// <param name="RelativePath">DW-relative path, e.g. "/Files/System/Log/ScheduledTasks".</param>
/// <param name="Bytes">Total bytes of all files directly in the folder.</param>
/// <param name="FileCount">Number of files directly in the folder.</param>
/// <param name="Oldest">Oldest file's last-write time.</param>
/// <param name="Newest">Newest file's last-write time.</param>
public sealed record StorageFolderSpec(
    string Name,
    string RelativePath,
    long Bytes,
    int FileCount,
    DateTime? Oldest,
    DateTime? Newest)
{
    /// <summary>Days between the oldest and newest file, i.e. how much history the folder keeps.</summary>
    public double SpanDays => Oldest is { } o && Newest is { } n && n > o ? (n - o).TotalDays : 0;
}

/// <summary>Size of one database table.</summary>
/// <param name="Name">Table name.</param>
/// <param name="RowCount">Rows in the heap/clustered index.</param>
/// <param name="Bytes">Reserved bytes across all its indexes.</param>
public sealed record TableSizeSpec(string Name, long RowCount, long Bytes);

/// <summary>
/// DW's log-retention configuration, read from GlobalSettings. When purging is off nothing
/// ever rotates — the single most common cause of runaway log folders and log tables.
/// </summary>
/// <param name="PurgeEnabled">/Globalsettings/Settings/Logging/FilesRetentionSettings/PurgeEnabled.</param>
/// <param name="FileLocations">Configured file locations, DW default is /System/Log and /System/Diagnostics.</param>
/// <param name="DbTables">Tables covered by the database retention settings.</param>
public sealed record RetentionSpec(
    bool PurgeEnabled,
    IReadOnlyList<string> FileLocations,
    IReadOnlyList<string> DbTables)
{
    public static RetentionSpec Unknown => new(false, [], []);

    public bool CoversTable(string tableName) =>
        DbTables.Any(t => string.Equals(t, tableName, StringComparison.OrdinalIgnoreCase));
}

/// <summary>One "who changed what" entry.</summary>
/// <param name="When">Timestamp of the change.</param>
/// <param name="Who">User name, or "unknown" when the source keeps no attribution.</param>
/// <param name="What">What was changed (command/type + name).</param>
/// <param name="Where">Which area/source the entry came from.</param>
public sealed record ChangeSpec(DateTime When, string Who, string What, string Where);
