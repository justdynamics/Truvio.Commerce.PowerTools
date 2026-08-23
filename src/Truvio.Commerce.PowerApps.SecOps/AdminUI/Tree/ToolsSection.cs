using Dynamicweb.Content.UI.Tree;
using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerApps.SecOps.AdminUI.Security;

namespace Truvio.Commerce.PowerApps.SecOps.AdminUI.Tree;

/// <summary>
/// Own tree section in the Content area, holding the SecOps tools. Discovered by DW's
/// AddInManager (any public NavigationSection with a NavigationContext ctor); addressed in
/// navigation paths by its type FullName because Id stays empty. Sort places it directly
/// above the Recycle bin section (which sits at int.MaxValue).
/// </summary>
public sealed class ToolsSection : ContentSectionBase
{
    public ToolsSection(NavigationContext context)
        : base(context)
    {
        Name = "Tools";
        Sort = int.MaxValue - 1;
    }

    /// <summary>Visible only with a selected website and Read on the SecOps function grant.</summary>
    public override bool ShouldShow() => base.ShouldShow() && SecOpsAccess.CanUseSecurityViewer();
}
