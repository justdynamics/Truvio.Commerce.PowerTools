using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Navigation;

public sealed class RecentChangeNavigationNodePathProvider : NavigationNodePathProvider<RecentChangeModel>
{
    public RecentChangeNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(RecentChangeModel? model) =>
        PowerToolsNavigationPaths.For<OperationsSection>(OperationsNodeProvider.ChangesNodeId);
}
