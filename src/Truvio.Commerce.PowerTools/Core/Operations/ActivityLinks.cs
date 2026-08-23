namespace Truvio.Commerce.PowerTools.Core.Operations;

/// <summary>
/// Resolves the scheduled-task ↔ data-integration-activity link in both directions.
/// <para>
/// DW stores the link one way only: a task running <c>JobScheduledTaskAddIn</c> keeps the
/// activity identifier in its add-in setting named "Activity". The identifier is
/// <c>group\name</c> for a grouped activity and just <c>name</c> for a root-level one
/// (<c>Job.GetJobIdentifier</c>). Nothing validates that the identifier still points at an
/// existing job file, which is why a renamed or deleted activity leaves a task that silently
/// fails — the case <see cref="Rules.BrokenActivityLinkRule"/> reports.
/// </para>
/// </summary>
public static class ActivityLinks
{
    /// <summary>Identifiers compare case-insensitively and treat / and \ as the same separator.</summary>
    public static string Normalise(string? activityId) =>
        (activityId ?? string.Empty).Replace('/', '\\').Trim().Trim('\\');

    /// <summary>Tasks that name an activity, keyed by the normalised activity id.</summary>
    public static ILookup<string, TaskSpec> TasksByActivity(IEnumerable<TaskSpec> tasks) =>
        tasks.Where(t => !string.IsNullOrWhiteSpace(t.LinkedActivityId))
             .ToLookup(t => Normalise(t.LinkedActivityId), StringComparer.OrdinalIgnoreCase);

    /// <summary>The tasks that run the given activity.</summary>
    public static IReadOnlyList<TaskSpec> TasksFor(IEnumerable<TaskSpec> tasks, ActivitySpec activity) =>
        TasksByActivity(tasks)[Normalise(activity.Id)].ToList();

    /// <summary>True when the task names an activity that no job file backs.</summary>
    public static bool IsBroken(TaskSpec task, IEnumerable<ActivitySpec> activities)
    {
        if (string.IsNullOrWhiteSpace(task.LinkedActivityId))
            return false;

        var wanted = Normalise(task.LinkedActivityId);
        return !activities.Any(a => string.Equals(Normalise(a.Id), wanted, StringComparison.OrdinalIgnoreCase));
    }
}
