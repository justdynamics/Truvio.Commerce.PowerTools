using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Application;
using Dynamicweb.CoreUI.Icons;
using Dynamicweb.CoreUI.Navigation;

namespace Truvio.Commerce.PowerTools.Shared.Navigation;

/// <summary>Serves the area's sections; every NavigationSection&lt;PowerToolsArea&gt; in any
/// loaded assembly is picked up automatically.</summary>
public sealed class PowerToolsAreaSectionProvider : NavigationSectionProvider<PowerToolsArea>
{
    public PowerToolsAreaSectionProvider(NavigationContext context)
        : base(context)
    {
    }
}
