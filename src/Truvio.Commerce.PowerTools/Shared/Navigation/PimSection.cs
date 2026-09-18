using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.Settings.Dw;
using Truvio.Commerce.PowerTools.Shared.Security;

namespace Truvio.Commerce.PowerTools.Shared.Navigation;

/// <summary>
/// "PIM" section of the PowerTools area — catalog data quality: what is incomplete, which
/// field to fix first, and which completion rules govern nothing.
/// </summary>
public sealed class PimSection : NavigationSection<PowerToolsArea>
{
    public PimSection(NavigationContext context)
        : base(context)
    {
        Name = "PIM";
        // Commerce is 30, Search 40, Operations 50.
        Sort = 35;
    }

    /// <summary>Visible only with Read on the PIM function grant.</summary>
    public override bool ShouldShow() =>
        PowerToolsAccess.CanUsePim() && DwPowerToolsSettings.Current.PimSectionEnabled;
}
