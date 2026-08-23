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

/// <summary>One activity: definition summary, the tasks that run it, and its newest run log.</summary>
public sealed class IntegrationActivityDetailScreen : OverviewScreenBase<IntegrationActivityDetailModel>
{
    protected override string GetScreenName() =>
        Model is null || string.IsNullOrEmpty(Model.Title) ? "Integration activity" : $"Activity: {Model.Title}";

    protected override void BuildOverviewScreen()
    {
        var model = Model;
        if (model is null)
            return;

        if (!string.IsNullOrEmpty(model.Error))
        {
            AddComponent(new Alert { Value = model.Error, Icon = Icon.ExclamationTriangle }, "Activity unavailable", Group.GroupWidth.Col_12);
            return;
        }

        SetInfobar(new InfoBar
        {
            Icon = Icon.ExchangeAlt,
            Information = new Dictionary<string, CardInfo.InfoValue>
            {
                ["Source"] = new(model.Source),
                ["Destination"] = new(model.Destination),
                ["Last run"] = new(model.LastRun),
                ["Result"] = new(new Badge
                {
                    Value = string.IsNullOrEmpty(model.LastResult) ? "Unknown" : model.LastResult,
                    BadgeType = model.LastResult switch
                    {
                        "Completed" => BadgeType.Success,
                        "CompletedWithError" => BadgeType.Warning,
                        "Failed" => BadgeType.Danger,
                        _ => BadgeType.Secondary
                    }
                })
            }
        });

        AddComponent(new HtmlBlock { Value = OpsHtml.Table(model.Definition) }, "Definition", Group.GroupWidth.Col_12);
        AddComponent(new HtmlBlock { Value = OpsHtml.Table(model.Tasks) }, "Scheduled by", Group.GroupWidth.Col_12);
        AddComponent(new HtmlBlock { Value = OpsHtml.Pre(model.LogTail) }, "Newest run log", Group.GroupWidth.Col_12);
    }

    protected override IEnumerable<ActionGroup>? GetScreenActions() =>
    [
        new()
        {
            Nodes =
            [
                new ActionNode
                {
                    Name = "All integration activities",
                    Icon = Icon.ExchangeAlt,
                    NodeAction = NavigateScreenAction.To<IntegrationActivityListScreen>().With(new IntegrationActivityListQuery())
                }
            ]
        }
    ];
}
