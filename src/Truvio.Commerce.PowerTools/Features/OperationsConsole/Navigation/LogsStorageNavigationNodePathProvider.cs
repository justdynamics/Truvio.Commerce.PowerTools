using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Navigation;

public sealed class LogsStorageNavigationNodePathProvider : NavigationNodePathProvider<LogsStorageModel>
{
    public LogsStorageNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(LogsStorageModel? model) =>
        PowerToolsNavigationPaths.For<OperationsSection>(OperationsNodeProvider.StorageNodeId);
}
