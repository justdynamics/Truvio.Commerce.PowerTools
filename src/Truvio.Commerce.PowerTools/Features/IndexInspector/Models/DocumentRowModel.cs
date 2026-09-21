using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Models;

/// <summary>One document row in the document browser list.</summary>
public sealed class DocumentRowModel : DataViewModelBase
{
    public string RepositoryName { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    public int Ordinal { get; set; }

    public string MatchKind { get; set; } = string.Empty;

    [ConfigurableProperty("Key", isSearchable: true)]
    public string Key { get; set; } = string.Empty;

    [ConfigurableProperty("Label", isSearchable: true)]
    public string Label { get; set; } = string.Empty;

    [ConfigurableProperty("Summary", isSearchable: true)]
    public string Summary { get; set; } = string.Empty;

    [ConfigurableProperty("Database")]
    public string Match { get; set; } = string.Empty;
}
