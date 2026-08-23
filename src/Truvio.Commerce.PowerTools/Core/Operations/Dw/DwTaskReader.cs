using System.Xml.Linq;
using Dynamicweb.Data;
using Dynamicweb.Scheduling;
using DwTask = Dynamicweb.Scheduling.Task;

namespace Truvio.Commerce.PowerTools.Core.Operations.Dw;

/// <summary>
/// Reads scheduled tasks and their run history out of DW.
/// <para>
/// Definitions come from <c>Dynamicweb.Scheduling.TaskService.GetTasks()</c> (cached, read-only).
/// Run history comes from the <c>ScheduledTaskExecution</c> table when the install has it —
/// it is the only place DW records <em>who</em> started a run — and falls back to
/// <c>TaskService.GetLastExecutionsLogs</c>, which reconstructs runs by scanning the log files
/// under /Files/System/Log/ScheduledTasks for the task's "Task 'x' with Id 'n'" marker.
/// </para>
/// </summary>
internal static class DwTaskReader
{
    /// <summary>The add-in setting a data-integration task stores its activity identifier in.</summary>
    private const string ActivityParameterName = "Activity";

    public static IReadOnlyList<TaskSpec> GetTasks()
    {
        try
        {
            return new TaskService().GetTasks().Select(ToSpec).OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch
        {
            return [];
        }
    }

    private static TaskSpec ToSpec(DwTask task)
    {
        var parameters = ParseParameters(task.AddInSettings);
        var activity = parameters.FirstOrDefault(p =>
            string.Equals(p.Name, ActivityParameterName, StringComparison.OrdinalIgnoreCase)).Value ?? string.Empty;

        return new TaskSpec(
            Id: task.ID,
            Name: task.Name ?? string.Empty,
            AddInTypeName: task.AddInTypeName ?? string.Empty,
            Enabled: task.Enabled,
            IntervalMinutes: task.Minute > 0 ? task.Minute : 0,
            ScheduleSummary: Safe(() => task.Schedule) ?? string.Empty,
            // DW stores "never" as its MinDate sentinel (2000-01-01); Task.LastRun already maps
            // that to null, and NextRun is only meaningful while the task is enabled.
            LastRun: task.LastRun,
            NextRun: task.Enabled && task.UpcomingRuntime > Consts.MinDate ? task.UpcomingRuntime : null,
            LastResult: task.LastResult,
            LastException: task.LastException ?? string.Empty,
            LinkedActivityId: activity,
            Comment: task.Comment ?? string.Empty);
    }

    public static IReadOnlyList<(string Name, string Value)> GetParameters(int taskId)
    {
        try
        {
            var task = new TaskService().GetTaskById(taskId);
            return task is null ? [] : ParseParameters(task.AddInSettings);
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// The add-in settings blob is DW's standard parameter XML:
    /// <c>&lt;Parameters&gt;&lt;Parameter name="x" value="y" /&gt;…&lt;/Parameters&gt;</c>.
    /// Longer values are written as element content instead of a value attribute.
    /// </summary>
    internal static List<(string Name, string Value)> ParseParameters(string? addInSettings)
    {
        if (string.IsNullOrWhiteSpace(addInSettings))
            return [];

        try
        {
            return XDocument.Parse(addInSettings)
                .Descendants()
                .Where(e => string.Equals(e.Name.LocalName, "Parameter", StringComparison.OrdinalIgnoreCase))
                .Select(e => (
                    Name: (string?)e.Attribute("name") ?? string.Empty,
                    Value: (string?)e.Attribute("value") ?? e.Value ?? string.Empty))
                .Where(p => !string.IsNullOrEmpty(p.Name))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    public static IReadOnlyList<TaskRunSpec> GetRuns(int taskId, int max)
    {
        var fromTable = ReadExecutionTable(taskId, max);
        return fromTable.Count > 0 ? fromTable : ReadLogLines(taskId, max);
    }

    /// <summary>
    /// ScheduledTaskExecution is the richest source — schedule/start/end, result and the user
    /// who triggered the run (null = the scheduler itself). It is not present on every DW10
    /// version, so its absence is not an error.
    /// </summary>
    private static List<TaskRunSpec> ReadExecutionTable(int taskId, int max)
    {
        var runs = new List<TaskRunSpec>();
        try
        {
            var sql = CommandBuilder.Create(
                """
                SELECT TOP ({0}) e.ScheduledTaskExecutionScheduleTime, e.ScheduledTaskExecutionStartTime,
                       e.ScheduledTaskExecutionEndTime, e.ScheduledTaskExecutionResult,
                       e.ScheduledTaskExecutionOutput, u.AccessUserUserName, u.AccessUserName
                FROM ScheduledTaskExecution e
                LEFT JOIN AccessUser u ON u.AccessUserId = e.ScheduledTaskExecutionUserId
                WHERE e.ScheduledTaskExecutionTaskId = {1}
                ORDER BY e.ScheduledTaskExecutionId DESC
                """,
                max,
                taskId);

            using var reader = Database.CreateDataReader(sql);
            while (reader.Read())
            {
                var userName = AsString(reader["AccessUserName"]);
                if (string.IsNullOrWhiteSpace(userName))
                    userName = AsString(reader["AccessUserUserName"]);

                runs.Add(new TaskRunSpec(
                    taskId,
                    AsDate(reader["ScheduledTaskExecutionScheduleTime"]),
                    AsDate(reader["ScheduledTaskExecutionStartTime"]),
                    AsDate(reader["ScheduledTaskExecutionEndTime"]),
                    string.IsNullOrWhiteSpace(userName) ? "Scheduler" : userName,
                    AsBool(reader["ScheduledTaskExecutionResult"]),
                    AsString(reader["ScheduledTaskExecutionOutput"])));
            }
        }
        catch
        {
            // No such table on this version, or no permission — the log-file fallback covers it.
            return [];
        }

        return runs;
    }

    /// <summary>
    /// Fallback for installs without the execution table: DW reconstructs runs from the
    /// scheduler log files. It carries no attribution, so "who ran this" reads "unknown".
    /// </summary>
    private static List<TaskRunSpec> ReadLogLines(int taskId, int max)
    {
        try
        {
            return new TaskService()
                .GetLastExecutionsLogs(taskId, max)
                .Select(line => new TaskRunSpec(
                    taskId,
                    null,
                    line.StartTime == default ? null : line.StartTime,
                    line.EndTime == default ? null : line.EndTime,
                    "unknown",
                    !line.IsErrorLine,
                    line.Message ?? string.Empty))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static string AsString(object? value) => value is null || value is DBNull ? string.Empty : value.ToString() ?? string.Empty;

    private static DateTime? AsDate(object? value) => value is null || value is DBNull ? null : Convert.ToDateTime(value);

    private static bool? AsBool(object? value) => value is null || value is DBNull ? null : Convert.ToBoolean(value);

    private static T? Safe<T>(Func<T> read)
    {
        try
        {
            return read();
        }
        catch
        {
            return default;
        }
    }
}
