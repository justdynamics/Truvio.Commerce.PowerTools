using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.BackendRights.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.BackendRights.Navigation;

/// <summary>Anchors the Backend Rights Viewer's screens under PowerTools ▸ Security ▸ Backend Rights.</summary>
public sealed class BackendUserNavigationNodePathProvider : NavigationNodePathProvider<BackendUserModel>
{
    public BackendUserNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(BackendUserModel? model) =>
        PowerToolsNavigationPaths.For(SecurityNodeProvider.BackendRightsNodeId);
}
