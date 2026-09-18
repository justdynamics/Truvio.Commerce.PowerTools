using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Models;

/// <summary>One "who changed what" row.</summary>
public sealed class RecentChangeModel : DataViewModelBase
{
    public string SourceKind { get; set; } = string.Empty;

    [ConfigurableProperty("When")]
    public string When { get; set; } = string.Empty;

    [ConfigurableProperty("Ago")]
    public string Ago { get; set; } = string.Empty;

    [ConfigurableProperty("Who", isSearchable: true)]
    public string Who { get; set; } = string.Empty;

    [ConfigurableProperty("What", isSearchable: true)]
    public string What { get; set; } = string.Empty;

    [ConfigurableProperty("Source", isSearchable: true)]
    public string Source { get; set; } = string.Empty;
}
