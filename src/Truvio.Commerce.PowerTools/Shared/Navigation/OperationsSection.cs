using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.Settings.Dw;
using Truvio.Commerce.PowerTools.Shared.Security;

namespace Truvio.Commerce.PowerTools.Shared.Navigation;

/// <summary>
/// "Operations" section of the PowerTools area — the read-only operations console: what runs,
/// what failed, what is growing, and who changed what.
/// </summary>
public sealed class OperationsSection : NavigationSection<PowerToolsArea>
{
    public OperationsSection(NavigationContext context)
        : base(context)
    {
        Name = "Operations";
        Sort = 30;
    }

    /// <summary>Visible only with Read on the Operations function grant.</summary>
    public override bool ShouldShow() =>
        PowerToolsAccess.CanUseOperations() && DwPowerToolsSettings.Current.OperationsSectionEnabled;
}
