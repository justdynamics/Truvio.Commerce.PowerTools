using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.AdminUI.Security;
using Truvio.Commerce.PowerTools.Core.Settings.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Tree;

/// <summary>
/// "Security" section of the PowerTools area, holding the security tools. Discovered by
/// DW's AddInManager (any public NavigationSection with a NavigationContext ctor);
/// addressed in navigation paths by its type FullName because Id stays empty. Future tool
/// families get their own sibling sections (Commerce, Operations, ...).
/// </summary>
public sealed class SecuritySection : NavigationSection<PowerToolsArea>
{
    public SecuritySection(NavigationContext context)
        : base(context)
    {
        Name = "Security";
        Sort = 10;
    }

    /// <summary>
    /// Visible with Read on any of the security tools' function grants — the Backend Rights Viewer
    /// carries its own, so a user granted only that still reaches the section.
    /// </summary>
    public override bool ShouldShow() =>
        (PowerToolsAccess.CanUseSecurityViewer() || PowerToolsAccess.CanUseBackendRights())
        && DwPowerToolsSettings.Current.SecuritySectionEnabled;
}
