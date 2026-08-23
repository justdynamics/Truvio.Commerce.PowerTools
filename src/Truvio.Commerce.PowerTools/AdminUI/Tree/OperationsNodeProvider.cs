using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Icons;
using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Truvio.Commerce.PowerTools.AdminUI.Screens;
using Truvio.Commerce.PowerTools.AdminUI.Security;

namespace Truvio.Commerce.PowerTools.AdminUI.Tree;

/// <summary>
/// Root nodes of the <see cref="OperationsSection"/>. Health comes first because it answers the
/// question the other four exist to detail.
/// </summary>
public sealed class OperationsNodeProvider : NavigationNodeProvider<OperationsSection>
{
    // Node IDs cannot contain '/' — DW NavigationNodePath splits on it.
    public const string HealthNodeId = "PowerTools_OpsHealth";
    public const string ScheduledTasksNodeId = "PowerTools_OpsScheduledTasks";
    public const string ActivitiesNodeId = "PowerTools_OpsActivities";
    public const string StorageNodeId = "PowerTools_OpsStorage";
    public const string ChangesNodeId = "PowerTools_OpsChanges";

    public override IEnumerable<NavigationNode> GetRootNodes()
    {
        if (!PowerToolsAccess.CanUseOperations())
            yield break;

        yield return new NavigationNode
        {
            Id = HealthNodeId,
            Name = "Health",
            Icon = Icon.Heartbeat,
            Sort = 5,
            HasSubNodes = false,
            NodeAction = NavigateScreenAction.To<OperationsHealthScreen>().With(new OperationsHealthQuery())
        };

        yield return new NavigationNode
        {
            Id = ScheduledTasksNodeId,
            Name = "Scheduled tasks",
            Icon = Icon.Schedule,
            Sort = 10,
            HasSubNodes = false,
            NodeAction = NavigateScreenAction.To<ScheduledTaskListScreen>().With(new ScheduledTaskListQuery())
        };

        yield return new NavigationNode
        {
            Id = ActivitiesNodeId,
            Name = "Integration activities",
            Icon = Icon.ExchangeAlt,
            Sort = 20,
            HasSubNodes = false,
            NodeAction = NavigateScreenAction.To<IntegrationActivityListScreen>().With(new IntegrationActivityListQuery())
        };

        yield return new NavigationNode
        {
            Id = StorageNodeId,
            Name = "Logs & storage",
            Icon = Icon.Database,
            Sort = 30,
            HasSubNodes = false,
            NodeAction = NavigateScreenAction.To<LogsStorageScreen>().With(new LogsStorageQuery())
        };

        yield return new NavigationNode
        {
            Id = ChangesNodeId,
            Name = "Recent changes",
            Icon = Icon.History,
            Sort = 40,
            HasSubNodes = false,
            NodeAction = NavigateScreenAction.To<RecentChangeListScreen>().With(new RecentChangeListQuery())
        };
    }

    public override IEnumerable<NavigationNode> GetSubNodes(NavigationNodePath parentNodePath)
    {
        yield break;
    }
}
