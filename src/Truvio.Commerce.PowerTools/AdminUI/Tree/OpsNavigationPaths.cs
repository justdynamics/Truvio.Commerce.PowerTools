using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.AdminUI.Models;

namespace Truvio.Commerce.PowerTools.AdminUI.Tree;

// Anchors every Operations screen under PowerTools ▸ Operations ▸ <its node>, so the tree keeps
// the right node highlighted when a screen is reached by a list action or a shared URL.

public sealed class OperationsHealthNavigationNodePathProvider : NavigationNodePathProvider<OperationsHealthModel>
{
    public OperationsHealthNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(OperationsHealthModel? model) =>
        PowerToolsNavigationPaths.For<OperationsSection>(OperationsNodeProvider.HealthNodeId);
}

public sealed class ScheduledTaskNavigationNodePathProvider : NavigationNodePathProvider<ScheduledTaskModel>
{
    public ScheduledTaskNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(ScheduledTaskModel? model) =>
        PowerToolsNavigationPaths.For<OperationsSection>(OperationsNodeProvider.ScheduledTasksNodeId);
}

public sealed class ScheduledTaskDetailNavigationNodePathProvider : NavigationNodePathProvider<ScheduledTaskDetailModel>
{
    public ScheduledTaskDetailNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(ScheduledTaskDetailModel? model) =>
        PowerToolsNavigationPaths.For<OperationsSection>(OperationsNodeProvider.ScheduledTasksNodeId);
}

public sealed class IntegrationActivityNavigationNodePathProvider : NavigationNodePathProvider<IntegrationActivityModel>
{
    public IntegrationActivityNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(IntegrationActivityModel? model) =>
        PowerToolsNavigationPaths.For<OperationsSection>(OperationsNodeProvider.ActivitiesNodeId);
}

public sealed class IntegrationActivityDetailNavigationNodePathProvider : NavigationNodePathProvider<IntegrationActivityDetailModel>
{
    public IntegrationActivityDetailNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(IntegrationActivityDetailModel? model) =>
        PowerToolsNavigationPaths.For<OperationsSection>(OperationsNodeProvider.ActivitiesNodeId);
}

public sealed class LogsStorageNavigationNodePathProvider : NavigationNodePathProvider<LogsStorageModel>
{
    public LogsStorageNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(LogsStorageModel? model) =>
        PowerToolsNavigationPaths.For<OperationsSection>(OperationsNodeProvider.StorageNodeId);
}

public sealed class RecentChangeNavigationNodePathProvider : NavigationNodePathProvider<RecentChangeModel>
{
    public RecentChangeNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(RecentChangeModel? model) =>
        PowerToolsNavigationPaths.For<OperationsSection>(OperationsNodeProvider.ChangesNodeId);
}
