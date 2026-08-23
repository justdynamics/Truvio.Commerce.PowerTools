using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.AdminUI.Security;

namespace Truvio.Commerce.PowerTools.AdminUI.Tree;

/// <summary>
/// "Settings" section of the PowerTools area — the suite's own configuration, last in the area
/// the way Settings is last in the admin. It is the only part of PowerTools that writes, and it
/// writes nothing but its own keys under <c>/Globalsettings/Truvio/PowerTools/</c>.
/// </summary>
public sealed class SettingsSection : NavigationSection<PowerToolsArea>
{
    public SettingsSection(NavigationContext context)
        : base(context)
    {
        Name = "Settings";
        Sort = 90;
    }

    /// <summary>Anyone who can use a tool may see how it is configured; changing needs Edit.</summary>
    public override bool ShouldShow() => PowerToolsAccess.CanViewSettings();
}
