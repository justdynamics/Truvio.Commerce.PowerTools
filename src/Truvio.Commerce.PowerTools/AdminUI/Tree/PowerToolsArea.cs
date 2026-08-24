using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Application;
using Dynamicweb.CoreUI.Icons;
using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Truvio.Commerce.PowerTools.AdminUI.Screens;

namespace Truvio.Commerce.PowerTools.AdminUI.Tree;

/// <summary>
/// Top-level "PowerTools" area in the admin navigation, holding every tool grouped into
/// sections (Security, ...). Discovered by DW's AddInManager like the built-in areas;
/// AreaBase requires the type name to end in "Area". No sub-areas — navigation paths under
/// this area carry <see cref="NavigationContext.Empty"/> as their context segment, the same
/// convention the Settings area uses. No [Licensable] attribute — its absence means the
/// area is not license-gated.
/// </summary>
public sealed class PowerToolsArea : AreaBase
{
    public PowerToolsArea()
    {
        Name = "PowerTools";
        Icon = Icon.Wrench;
        // Between Apps (90) and Settings (100).
        Sort = 95;
        // Landing screen when the area itself is clicked.
        SecondaryAction = NavigateScreenAction.To<AccessOverviewScreen>().With(new AccessOverviewQuery());
    }
}

/// <summary>Serves the area's sections; every NavigationSection&lt;PowerToolsArea&gt; in any
/// loaded assembly is picked up automatically.</summary>
public sealed class PowerToolsAreaSectionProvider : NavigationSectionProvider<PowerToolsArea>
{
    public PowerToolsAreaSectionProvider(NavigationContext context)
        : base(context)
    {
    }
}
