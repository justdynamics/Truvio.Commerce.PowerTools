using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.AdminUI.Models;

namespace Truvio.Commerce.PowerTools.AdminUI.Tree;

// Anchors every PIM-section screen (and its drilldowns) under the node it belongs to, so the
// tree keeps the right node highlighted while the user drills in.

public sealed class PimQualityNavigationNodePathProvider : NavigationNodePathProvider<PimQualityModel>
{
    public PimQualityNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(PimQualityModel? model) =>
        PowerToolsNavigationPaths.For<PimSection>(PimNodeProvider.QualityNodeId);
}

public sealed class PimCompletenessNavigationNodePathProvider : NavigationNodePathProvider<PimCompletenessModel>
{
    public PimCompletenessNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(PimCompletenessModel? model) =>
        PowerToolsNavigationPaths.For<PimSection>(PimNodeProvider.CompletenessNodeId);
}

/// <summary>The per-product drill-down belongs to the explorer it was opened from.</summary>
public sealed class PimProductQualityNavigationNodePathProvider : NavigationNodePathProvider<PimProductQualityModel>
{
    public PimProductQualityNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(PimProductQualityModel? model) =>
        PowerToolsNavigationPaths.For<PimSection>(PimNodeProvider.CompletenessNodeId);
}

public sealed class PimGovernanceNavigationNodePathProvider : NavigationNodePathProvider<PimGovernanceModel>
{
    public PimGovernanceNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(PimGovernanceModel? model) =>
        PowerToolsNavigationPaths.For<PimSection>(PimNodeProvider.GovernanceNodeId);
}
