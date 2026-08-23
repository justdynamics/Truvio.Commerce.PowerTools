using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.AdminUI.Models;

/// <summary>
/// One row in the page-audience drilldown: the page itself, then each grid row and paragraph,
/// with what the selected account experiences.
/// </summary>
public sealed class AudienceItemModel : DataViewModelBase
{
    public bool VisibleState { get; set; }

    public int LevelValue { get; set; }

    public string OriginKind { get; set; } = string.Empty;

    [ConfigurableProperty("Content", isSearchable: true)]
    public string ItemType { get; set; } = string.Empty;

    [ConfigurableProperty("Name", isSearchable: true)]
    public string Name { get; set; } = string.Empty;

    [ConfigurableProperty("Sees it")]
    public string Visible { get; set; } = string.Empty;

    [ConfigurableProperty("Level")]
    public string Level { get; set; } = string.Empty;

    [ConfigurableProperty("Why", isSearchable: true)]
    public string Reason { get; set; } = string.Empty;
}
