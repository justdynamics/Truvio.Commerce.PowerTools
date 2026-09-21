using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.PimQuality.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Navigation;

public sealed class PimCompletenessNavigationNodePathProvider : NavigationNodePathProvider<PimCompletenessModel>
{
    public PimCompletenessNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(PimCompletenessModel? model) =>
        PowerToolsNavigationPaths.For<PimSection>(PimNodeProvider.CompletenessNodeId);
}
