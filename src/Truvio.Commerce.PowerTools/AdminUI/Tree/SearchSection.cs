using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.AdminUI.Security;
using Truvio.Commerce.PowerTools.Core.Settings.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Tree;

/// <summary>
/// "Search" section of the PowerTools area — the Index &amp; Query Inspector: what is in the
/// repositories, what is stale, and which query will misbehave.
/// </summary>
public sealed class SearchSection : NavigationSection<PowerToolsArea>
{
    public SearchSection(NavigationContext context)
        : base(context)
    {
        Name = "Search";
        Sort = 40;
    }

    /// <summary>Visible only with Read on the Index &amp; Query Inspector function grant.</summary>
    public override bool ShouldShow() =>
        PowerToolsAccess.CanUseSearchInspector() && DwPowerToolsSettings.Current.SearchSectionEnabled;
}
