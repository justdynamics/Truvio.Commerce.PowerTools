using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Navigation;

public sealed class ScheduledTaskNavigationNodePathProvider : NavigationNodePathProvider<ScheduledTaskModel>
{
    public ScheduledTaskNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(ScheduledTaskModel? model) =>
        PowerToolsNavigationPaths.For<OperationsSection>(OperationsNodeProvider.ScheduledTasksNodeId);
}
