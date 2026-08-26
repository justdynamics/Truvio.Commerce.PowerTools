using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Displays.Widgets;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Truvio.Commerce.PowerTools.AdminUI.Security;
using Icon = Dynamicweb.CoreUI.Icons.Icon;

namespace Truvio.Commerce.PowerTools.AdminUI.Screens;

/// <summary>
/// Step 1 of the Backend Rights Viewer: pick the backend user to explain. A list screen, so the
/// toolbar search and paging come for free.
/// </summary>
public sealed class BackendRightsListScreen : ListScreenBase<BackendUserModel>
{
    private BackendRightsListQuery Q => Query as BackendRightsListQuery ?? new BackendRightsListQuery();

    protected override string GetScreenName() => "Accounts";

    protected override IEnumerable<ListViewMapping> GetViewMappings() =>
    [
        new RowViewMapping
        {
            Columns =
            [
                CreateMapping(m => m.Name),
                CreateMapping(m => m.UserName),
                CreateMapping(m => m.BackendAccess),
                CreateMapping(m => m.Status)
            ]
        }
    ];

    protected override ActionBase? GetListItemPrimaryAction(BackendUserModel model)
    {
        if (!PowerToolsAccess.CanUseBackendRights() || string.IsNullOrEmpty(model.AccountKey))
            return null;

        return NavigateScreenAction.To<BackendRightsScreen>()
            .With(new BackendRightsQuery { AccountKey = model.AccountKey });
    }

    protected override Cell? GetCell(string propertyName, BackendUserModel model)
    {
        if (string.IsNullOrEmpty(model.AccountKey))
            return null;

        return propertyName switch
        {
            nameof(BackendUserModel.BackendAccess) when !string.IsNullOrEmpty(model.BackendAccess) =>
                Badges.Visible(model.BackendAccess == "Yes", model.BackendAccess),
            nameof(BackendUserModel.Status) =>
                Badges.AccountKind(model.Status switch
                {
                    "Elevated" => "Role",
                    "Administrator" => "Group",
                    _ => "User"
                }) is { } cell && model.Status is "Elevated" or "Administrator" ? cell : null,
            _ => null
        };
    }

    protected override IEnumerable<ActionGroup>? GetScreenActions() =>
    [
        new()
        {
            Nodes =
            [
                new ActionNode
                {
                    Name = Q.ShowWithoutAccess ? "Hide users without backend access" : "Show users without backend access",
                    Icon = Icon.UserCircle,
                    NodeAction = NavigateScreenAction.To<BackendRightsListScreen>()
                        .With(new BackendRightsListQuery { ShowWithoutAccess = !Q.ShowWithoutAccess })
                        .WithForceReload()
                }
            ]
        }
    ];
}

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

/// <summary>The "Why?" slide-over: one area, section or node explained in full.</summary>
public sealed class BackendRightsWhyScreen : OverviewScreenBase<BackendRightsWhyModel>
{
    protected override string GetScreenName() => Model?.Heading ?? "Why?";

    protected override void BuildOverviewScreen()
    {
        if (Model is null)
            return;

        // The heading is already the screen name — an unnamed group avoids showing it twice.
        AddComponent(new HtmlBlock { Value = Model.Html }, string.Empty, Group.GroupWidth.Col_12);
    }
}
