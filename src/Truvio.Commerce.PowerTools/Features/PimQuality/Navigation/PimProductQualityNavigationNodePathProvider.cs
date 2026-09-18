using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.PimQuality.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Navigation;

/// <summary>The per-product drill-down belongs to the explorer it was opened from.</summary>
public sealed class PimProductQualityNavigationNodePathProvider : NavigationNodePathProvider<PimProductQualityModel>
{
    public PimProductQualityNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(PimProductQualityModel? model) =>
        PowerToolsNavigationPaths.For<PimSection>(PimNodeProvider.CompletenessNodeId);
}
