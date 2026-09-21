namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Core;

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
