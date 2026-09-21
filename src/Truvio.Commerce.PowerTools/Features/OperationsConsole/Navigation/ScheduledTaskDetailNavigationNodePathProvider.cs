using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Navigation;

public sealed class ScheduledTaskDetailNavigationNodePathProvider : NavigationNodePathProvider<ScheduledTaskDetailModel>
{
    public ScheduledTaskDetailNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(ScheduledTaskDetailModel? model) =>
        PowerToolsNavigationPaths.For<OperationsSection>(OperationsNodeProvider.ScheduledTasksNodeId);
}
