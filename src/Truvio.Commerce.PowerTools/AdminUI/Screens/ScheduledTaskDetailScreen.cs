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
/// One scheduled task: definition, saved add-in parameters, and the last runs. An overview
/// screen rather than a grid — run messages and exception text are long.
/// </summary>
public sealed class ScheduledTaskDetailScreen : OverviewScreenBase<ScheduledTaskDetailModel>
{
    protected override string GetScreenName() =>
        Model is null || string.IsNullOrEmpty(Model.Title) ? "Scheduled task" : $"Task: {Model.Title}";

    protected override void BuildOverviewScreen()
    {
        var model = Model;
        if (model is null)
            return;

        if (!string.IsNullOrEmpty(model.Error))
        {
            AddComponent(new Alert { Value = model.Error, Icon = Icon.ExclamationTriangle }, "Task unavailable", Group.GroupWidth.Col_12);
            return;
        }

        SetInfobar(new InfoBar
        {
            Icon = Icon.Schedule,
            Information = new Dictionary<string, CardInfo.InfoValue>
            {
                ["Add-in"] = new(model.AddIn),
                ["State"] = new(new Badge
                {
                    Value = model.Status,
                    BadgeType = model.State switch
                    {
                        "failed" => BadgeType.Danger,
                        "disabled" => BadgeType.Secondary,
                        _ => BadgeType.Success
                    }
                }),
                ["Last run"] = new(model.LastRun),
                ["Next run"] = new(model.NextRun)
            }
        });

        AddComponent(new HtmlBlock { Value = OpsHtml.Table(model.Definition) }, "Definition", Group.GroupWidth.Col_12);

        if (!string.IsNullOrWhiteSpace(model.LastException))
            AddComponent(new HtmlBlock { Value = OpsHtml.Pre([model.LastException]) }, "Last exception", Group.GroupWidth.Col_12);

        AddComponent(new HtmlBlock { Value = OpsHtml.Table(model.Parameters) }, "Saved parameters (read-only)", Group.GroupWidth.Col_12);

        var runs = model.Runs.Count == 0
            ? OpsHtml.Note(model.RunSourceNote)
            : OpsHtml.Table(model.Runs) + OpsHtml.Note(model.RunSourceNote);
        AddComponent(new HtmlBlock { Value = runs }, $"Last {ScheduledTaskDetailQuery.RunCount} runs", Group.GroupWidth.Col_12);
    }

    protected override IEnumerable<ActionGroup>? GetScreenActions() =>
    [
        new()
        {
            Nodes =
            [
                new ActionNode
                {
                    Name = "All scheduled tasks",
                    Icon = Icon.Schedule,
                    NodeAction = NavigateScreenAction.To<ScheduledTaskListScreen>().With(new ScheduledTaskListQuery())
                }
            ]
        }
    ];
}
