using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Navigation;

// Anchors every Operations screen under PowerTools ▸ Operations ▸ <its node>, so the tree keeps
// the right node highlighted when a screen is reached by a list action or a shared URL.

public sealed class OperationsHealthNavigationNodePathProvider : NavigationNodePathProvider<OperationsHealthModel>
{
    public OperationsHealthNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(OperationsHealthModel? model) =>
        PowerToolsNavigationPaths.For<OperationsSection>(OperationsNodeProvider.HealthNodeId);
}
