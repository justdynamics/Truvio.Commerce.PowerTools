using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.PimQuality.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Navigation;

// Anchors every PIM-section screen (and its drilldowns) under the node it belongs to, so the
// tree keeps the right node highlighted while the user drills in.

public sealed class PimQualityNavigationNodePathProvider : NavigationNodePathProvider<PimQualityModel>
{
    public PimQualityNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(PimQualityModel? model) =>
        PowerToolsNavigationPaths.For<PimSection>(PimNodeProvider.QualityNodeId);
}
