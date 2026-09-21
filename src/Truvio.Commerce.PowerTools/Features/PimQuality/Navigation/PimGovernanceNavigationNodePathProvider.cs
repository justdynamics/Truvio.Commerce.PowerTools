using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.PimQuality.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Navigation;

public sealed class PimGovernanceNavigationNodePathProvider : NavigationNodePathProvider<PimGovernanceModel>
{
    public PimGovernanceNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(PimGovernanceModel? model) =>
        PowerToolsNavigationPaths.For<PimSection>(PimNodeProvider.GovernanceNodeId);
}
