using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Navigation;

public sealed class IntegrationActivityNavigationNodePathProvider : NavigationNodePathProvider<IntegrationActivityModel>
{
    public IntegrationActivityNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(IntegrationActivityModel? model) =>
        PowerToolsNavigationPaths.For<OperationsSection>(OperationsNodeProvider.ActivitiesNodeId);
}
