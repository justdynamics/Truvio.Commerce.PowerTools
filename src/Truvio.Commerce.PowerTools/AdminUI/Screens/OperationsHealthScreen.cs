using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Displays.Widgets;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Icon = Dynamicweb.CoreUI.Icons.Icon;

namespace Truvio.Commerce.PowerTools.AdminUI.Screens;

/// <summary>
/// Operations landing screen: the health verdict and the counts in the info bar, then every
/// finding the rules produced, worst first.
/// </summary>
public sealed class OperationsHealthScreen : OverviewScreenBase<OperationsHealthModel>
{
    protected override string GetScreenName() => "Overview";

    protected override void BuildOverviewScreen()
    {
        var model = Model;
        if (model is null)
            return;

        if (!string.IsNullOrEmpty(model.Error))
        {
            AddComponent(new Alert { Value = model.Error, Icon = Icon.ExclamationTriangle }, "Health check failed", Group.GroupWidth.Col_12);
            return;
        }

        SetInfobar(new InfoBar
        {
            Icon = Icon.Heartbeat,
            Information = new Dictionary<string, CardInfo.InfoValue>
            {
                ["Install"] = new(new Badge
                {
                    Value = model.Verdict,
                    BadgeType = model.Healthy ? BadgeType.Success : BadgeType.Warning
                }),
                ["Scheduled tasks"] = new(model.Tasks),
                ["Failing"] = new(model.FailingTasks),
                ["Stale"] = new(model.StaleTasks),
                ["Broken activity links"] = new(model.BrokenLinks),
                ["Storage"] = new(model.Storage),
                ["Largest growth"] = new(model.LargestBloat),
                ["Findings"] = new(model.FindingCounts)
            }
        });

        AddComponent(new HtmlBlock { Value = OpsHtml.Table(model.Findings) }, "Findings", Group.GroupWidth.Col_12);
    }

    protected override IEnumerable<ActionGroup>? GetScreenActions() =>
    [
        new()
        {
            Nodes =
            [
                new ActionNode
                {
                    Name = "Scheduled tasks",
                    Icon = Icon.Schedule,
                    NodeAction = NavigateScreenAction.To<ScheduledTaskListScreen>().With(new ScheduledTaskListQuery())
                },
                new ActionNode
                {
                    Name = "Integration activities",
                    Icon = Icon.ExchangeAlt,
                    NodeAction = NavigateScreenAction.To<IntegrationActivityListScreen>().With(new IntegrationActivityListQuery())
                },
                new ActionNode
                {
                    Name = "Logs & storage",
                    Icon = Icon.Database,
                    NodeAction = NavigateScreenAction.To<LogsStorageScreen>().With(new LogsStorageQuery())
                },
                new ActionNode
                {
                    Name = "Recent changes",
                    Icon = Icon.History,
                    NodeAction = NavigateScreenAction.To<RecentChangeListScreen>().With(new RecentChangeListQuery())
                }
            ]
        }
    ];
}
