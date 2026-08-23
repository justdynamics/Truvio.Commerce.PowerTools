using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.AdminUI.Security;
using Truvio.Commerce.PowerTools.Core.Settings.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Tree;

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
