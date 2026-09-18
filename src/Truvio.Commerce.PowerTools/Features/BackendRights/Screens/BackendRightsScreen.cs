using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Displays.Widgets;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Screens;
using Icon = Dynamicweb.CoreUI.Icons.Icon;
using Truvio.Commerce.PowerTools.Features.BackendRights.Models;
using Truvio.Commerce.PowerTools.Features.BackendRights.Queries;

namespace Truvio.Commerce.PowerTools.Features.BackendRights.Screens;

/// <summary>
/// Step 2: the report. An overview screen — the list grid splits width evenly and clips long text,
/// and every column here is an explanation.
/// </summary>
public sealed class BackendRightsScreen : OverviewScreenBase<BackendRightsModel>
{
    protected override string GetScreenName() =>
        Model is null || string.IsNullOrEmpty(Model.Title) ? "Backend rights" : Model.Title;

    protected override void BuildOverviewScreen()
    {
        var model = Model;
        if (model is null)
            return;

        if (!string.IsNullOrEmpty(model.Error))
        {
            AddComponent(new Alert { Value = model.Error, Icon = Icon.ExclamationTriangle }, "Report failed", Group.GroupWidth.Col_12);
            return;
        }

        SetInfobar(new InfoBar
        {
            Icon = Icon.Shield,
            Information = new Dictionary<string, CardInfo.InfoValue>
            {
                ["User"] = new(model.Title),
                ["Backend access"] = new(new Badge
                {
                    Value = model.BackendAccess ? "Yes" : "No",
                    BadgeType = model.BackendAccess ? BadgeType.Success : BadgeType.Danger
                }),
                ["Effective status"] = new(model.Status),
                ["Areas visible"] = new(model.AreasVisible),
                ["Gate in force"] = new(model.GateInForce)
            }
        });

        foreach (var section in model.Sections)
            AddComponent(new HtmlBlock { Value = section.Html }, section.Heading, Group.GroupWidth.Col_12);
    }

    protected override IEnumerable<ActionGroup>? GetScreenActions() =>
    [
        new()
        {
            Nodes =
            [
                new ActionNode
                {
                    Name = "Select another user",
                    Icon = Icon.UserCircle,
                    NodeAction = NavigateScreenAction.To<BackendRightsListScreen>()
                        .With(new BackendRightsListQuery())
                }
            ]
        }
    ];
}
