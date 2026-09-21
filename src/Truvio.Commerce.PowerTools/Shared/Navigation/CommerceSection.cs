using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.Settings.Dw;
using Truvio.Commerce.PowerTools.Shared.Security;

namespace Truvio.Commerce.PowerTools.Shared.Navigation;

/// <summary>"Commerce" section of the PowerTools area — the commerce explainers.</summary>
public sealed class CommerceSection : NavigationSection<PowerToolsArea>
{
    public CommerceSection(NavigationContext context)
        : base(context)
    {
        Name = "Commerce";
        Sort = 20;
    }

    /// <summary>Visible only with Read on the Price Explainer function grant.</summary>
    public override bool ShouldShow() =>
        PowerToolsAccess.CanUsePriceExplainer() && DwPowerToolsSettings.Current.CommerceSectionEnabled;
}
