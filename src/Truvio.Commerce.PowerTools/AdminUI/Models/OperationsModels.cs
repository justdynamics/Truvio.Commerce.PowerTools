using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.AdminUI.Models;

/// <summary>
/// One scheduled task row. The list grid gives every column the same width and clips what does
/// not fit, so the columns are few and short; everything long lives on the detail screen.
/// </summary>
public sealed class ScheduledTaskModel : DataViewModelBase
{
    public int TaskId { get; set; }

    /// <summary>Drives the state badge; not a column of its own.</summary>
    public string State { get; set; } = string.Empty;

    [ConfigurableProperty("Task")]
    public string Name { get; set; } = string.Empty;

    [ConfigurableProperty("Add-in")]
    public string AddIn { get; set; } = string.Empty;

    [ConfigurableProperty("Runs")]
    public string Schedule { get; set; } = string.Empty;

    [ConfigurableProperty("Status")]
    public string Status { get; set; } = string.Empty;

    [ConfigurableProperty("Last run")]
    public string LastRun { get; set; } = string.Empty;

    [ConfigurableProperty("Next run")]
    public string NextRun { get; set; } = string.Empty;
}

/// <summary>The task detail report — rendered as sections, not as a grid.</summary>
public sealed class ScheduledTaskDetailModel : DataViewModelBase
{
    public int TaskId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string AddIn { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string LastRun { get; set; } = string.Empty;

    public string NextRun { get; set; } = string.Empty;

    public string Error { get; set; } = string.Empty;

    public List<OpsRowModel> Definition { get; set; } = [];

    public List<OpsRowModel> Parameters { get; set; } = [];

    public List<OpsRowModel> Runs { get; set; } = [];

    public string RunSourceNote { get; set; } = string.Empty;

    public string LastException { get; set; } = string.Empty;
}

/// <summary>One data-integration activity row.</summary>
public sealed class IntegrationActivityModel : DataViewModelBase
{
    public string ActivityId { get; set; } = string.Empty;

    public string ResultKind { get; set; } = string.Empty;

    [ConfigurableProperty("Activity")]
    public string Name { get; set; } = string.Empty;

    [ConfigurableProperty("Source")]
    public string Source { get; set; } = string.Empty;

    [ConfigurableProperty("Destination")]
    public string Destination { get; set; } = string.Empty;

    [ConfigurableProperty("Scheduled by")]
    public string ScheduledBy { get; set; } = string.Empty;

    [ConfigurableProperty("Last run")]
    public string LastRun { get; set; } = string.Empty;

    [ConfigurableProperty("Result")]
    public string LastResult { get; set; } = string.Empty;
}

/// <summary>The activity detail report.</summary>
public sealed class IntegrationActivityDetailModel : DataViewModelBase
{
    public string ActivityId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public string LastRun { get; set; } = string.Empty;

    public string LastResult { get; set; } = string.Empty;

    public string Error { get; set; } = string.Empty;

    public List<OpsRowModel> Definition { get; set; } = [];

    public List<OpsRowModel> Tasks { get; set; } = [];

    public List<string> LogTail { get; set; } = [];
}

/// <summary>The logs &amp; storage report.</summary>
public sealed class LogsStorageModel : DataViewModelBase
{
    public string Error { get; set; } = string.Empty;

    public string LogTotal { get; set; } = string.Empty;

    public string DatabaseTotal { get; set; } = string.Empty;

    public string RetentionSummary { get; set; } = string.Empty;

    public bool RetentionEnabled { get; set; }

    public int FindingCount { get; set; }

    public List<OpsRowModel> Folders { get; set; } = [];

    public List<OpsRowModel> Tables { get; set; } = [];

    public List<OpsRowModel> Retention { get; set; } = [];

    public List<OpsRowModel> Findings { get; set; } = [];
}

/// <summary>The health headline.</summary>
public sealed class OperationsHealthModel : DataViewModelBase
{
    public string Error { get; set; } = string.Empty;

    public string Verdict { get; set; } = string.Empty;

    public bool Healthy { get; set; }

    public string Tasks { get; set; } = string.Empty;

    public string FailingTasks { get; set; } = string.Empty;

    public string StaleTasks { get; set; } = string.Empty;

    public string BrokenLinks { get; set; } = string.Empty;

    public string Storage { get; set; } = string.Empty;

    public string LargestBloat { get; set; } = string.Empty;

    /// <summary>e.g. "0 critical, 2 warning, 1 info" — keeps "Healthy" honest next to a long list.</summary>
    public string FindingCounts { get; set; } = string.Empty;

    public List<OpsRowModel> Findings { get; set; } = [];
}

/// <summary>One "who changed what" row.</summary>
public sealed class RecentChangeModel : DataViewModelBase
{
    public string SourceKind { get; set; } = string.Empty;

    [ConfigurableProperty("When")]
    public string When { get; set; } = string.Empty;

    [ConfigurableProperty("Ago")]
    public string Ago { get; set; } = string.Empty;

    [ConfigurableProperty("Who")]
    public string Who { get; set; } = string.Empty;

    [ConfigurableProperty("What")]
    public string What { get; set; } = string.Empty;

    [ConfigurableProperty("Source")]
    public string Source { get; set; } = string.Empty;
}

/// <summary>
/// A single line in one of the Operations report tables: a label, an optional badge, a value
/// and an explanation. The same shape the Price Explainer report uses, so the two tools read
/// the same way.
/// </summary>
public sealed class OpsRowModel
{
    public string Item { get; set; } = string.Empty;

    public string Verdict { get; set; } = string.Empty;

    /// <summary>win / ok / match / reject / warn / info — drives the badge colour.</summary>
    public string VerdictKind { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string Why { get; set; } = string.Empty;
}
