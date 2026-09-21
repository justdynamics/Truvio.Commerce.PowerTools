using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Selectors;

/// <summary>One product-group row in the PIM scope picker.</summary>
public sealed class PimGroupPickModel : DataViewModelBase
{
    public string GroupId { get; set; } = string.Empty;

    [ConfigurableProperty("Group", isSearchable: true)]
    public string Name { get; set; } = string.Empty;

    [ConfigurableProperty("Id", isSearchable: true)]
    public string Id { get; set; } = string.Empty;
}
