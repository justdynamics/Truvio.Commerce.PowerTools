using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Models;

/// <summary>One field row in "Field where-used".</summary>
public sealed class FieldUsageModel : DataViewModelBase
{
    public string StatusKind { get; set; } = string.Empty;

    [ConfigurableProperty("Field", isSearchable: true)]
    public string Field { get; set; } = string.Empty;

    [ConfigurableProperty("Index", isSearchable: true)]
    public string Index { get; set; } = string.Empty;

    /// <summary>Type and index flags in one column — the list grid gives every column the
    /// same width, so five columns is the readable maximum here.</summary>
    [ConfigurableProperty("Type", isSearchable: true)]
    public string Type { get; set; } = string.Empty;

    [ConfigurableProperty("Used by", isSearchable: true)]
    public string UsedBy { get; set; } = string.Empty;

    [ConfigurableProperty("Status", isSearchable: true)]
    public string Status { get; set; } = string.Empty;
}
