using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.ContentAccess.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.ContentAccess.Navigation;

/// <summary>Anchors the account picker (and its drilldowns) under PowerTools > Security > Security Viewer.</summary>

public sealed class AccessNodeNavigationNodePathProvider : NavigationNodePathProvider<AccessNodeModel>
{
    public AccessNodeNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(AccessNodeModel? model) =>
        PowerToolsNavigationPaths.For(SecurityNodeProvider.SecurityViewerNodeId);
}
