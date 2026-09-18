using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Models;

/// <summary>
/// One scheduled task row. The list grid gives every column the same width and clips what does
/// not fit, so the columns are few and short; everything long lives on the detail screen.
/// </summary>
public sealed class ScheduledTaskModel : DataViewModelBase
{
    public int TaskId { get; set; }

    /// <summary>Drives the state badge; not a column of its own.</summary>
    public string State { get; set; } = string.Empty;

    [ConfigurableProperty("Task", isSearchable: true)]
    public string Name { get; set; } = string.Empty;

    [ConfigurableProperty("Add-in", isSearchable: true)]
    public string AddIn { get; set; } = string.Empty;

    [ConfigurableProperty("Runs")]
    public string Schedule { get; set; } = string.Empty;

    [ConfigurableProperty("Status", isSearchable: true)]
    public string Status { get; set; } = string.Empty;

    [ConfigurableProperty("Last run")]
    public string LastRun { get; set; } = string.Empty;

    [ConfigurableProperty("Next run")]
    public string NextRun { get; set; } = string.Empty;
}
