using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.BackendRights.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.BackendRights.Navigation;

public sealed class BackendRightsNavigationNodePathProvider : NavigationNodePathProvider<BackendRightsModel>
{
    public BackendRightsNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(BackendRightsModel? model) =>
        PowerToolsNavigationPaths.For(SecurityNodeProvider.BackendRightsNodeId);
}
