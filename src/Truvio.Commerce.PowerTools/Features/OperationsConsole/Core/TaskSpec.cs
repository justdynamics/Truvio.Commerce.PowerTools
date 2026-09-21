namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Core;

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
