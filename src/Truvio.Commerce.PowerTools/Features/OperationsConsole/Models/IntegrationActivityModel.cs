using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Models;

/// <summary>One data-integration activity row.</summary>
public sealed class IntegrationActivityModel : DataViewModelBase
{
    public string ActivityId { get; set; } = string.Empty;

    public string ResultKind { get; set; } = string.Empty;

    [ConfigurableProperty("Activity", isSearchable: true)]
    public string Name { get; set; } = string.Empty;

    [ConfigurableProperty("Source", isSearchable: true)]
    public string Source { get; set; } = string.Empty;

    [ConfigurableProperty("Destination", isSearchable: true)]
    public string Destination { get; set; } = string.Empty;

    [ConfigurableProperty("Scheduled by", isSearchable: true)]
    public string ScheduledBy { get; set; } = string.Empty;

    [ConfigurableProperty("Last run")]
    public string LastRun { get; set; } = string.Empty;

    [ConfigurableProperty("Result", isSearchable: true)]
    public string LastResult { get; set; } = string.Empty;
}
