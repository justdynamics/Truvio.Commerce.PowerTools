using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Displays.Widgets;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Icon = Dynamicweb.CoreUI.Icons.Icon;
using Truvio.Commerce.PowerTools.Features.BackendRights.Models;
using Truvio.Commerce.PowerTools.Features.BackendRights.Queries;
using Truvio.Commerce.PowerTools.Shared.AdminUI;
using Truvio.Commerce.PowerTools.Shared.Security;

namespace Truvio.Commerce.PowerTools.Features.BackendRights.Screens;

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
            nameof(BackendUserModel.Status) => Badges.BackendStatus(model.Status),
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
