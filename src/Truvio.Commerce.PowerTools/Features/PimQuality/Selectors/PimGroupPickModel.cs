using Dynamicweb.CoreUI;
using Dynamicweb.CoreUI.Actions.Implementations.Components.Selector;
using Dynamicweb.CoreUI.Data;
using Dynamicweb.CoreUI.Data.Filtering;
using Dynamicweb.CoreUI.Editors.Selectors;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;

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
