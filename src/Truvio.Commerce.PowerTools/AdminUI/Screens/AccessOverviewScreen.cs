using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Icons;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Truvio.Commerce.PowerTools.Core.Principals;
using Truvio.Commerce.PowerTools.Core.Principals.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Screens;

/// <summary>
/// Account -> content tree: every page with the effective level the selected account
/// resolves to, where that level comes from, and gating warnings.
/// </summary>
public sealed class AccessOverviewScreen : ListScreenBase<AccessNodeModel>
{
    private SecurityAccount? _account;

    private SecurityAccount? Account => _account ??=
        (Query as AccessOverviewQuery)?.AccountKey is { Length: > 0 } key
            ? new DwAccountCatalog().Resolve(key)
            : null;

    protected override string GetScreenName() =>
        Account is null
            ? "Security Viewer - content access"
            : $"Content access: {Account.DisplayName}";

#if DW_HAS_SCREEN_EXPLANATION
    protected override string? GetScreenExplanation() =>
        Account is null ? null : $"What '{Account.DisplayName}' can see, page by page";
#endif

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
                    NodeAction = NavigateScreenAction.To<AccountListScreen>()
                        .With(new AccountListQuery())
                }
            ]
        }
    ];

    protected override IEnumerable<ListViewMapping> GetViewMappings() =>
    [
        new RowViewMapping
        {
            Columns =
            [
                CreateMapping(m => m.Name),
                CreateMapping(m => m.Visible),
                CreateMapping(m => m.Level),
                CreateMapping(m => m.Origin),
                CreateMapping(m => m.Warning)
            ]
        }
    ];

    protected override Cell? GetCell(string propertyName, AccessNodeModel model)
    {
        if (model.PageId <= 0)
            return null; // website header row stays plain

        return propertyName switch
        {
            nameof(AccessNodeModel.Visible) when model.VisibleState is bool visible =>
                Badges.Visible(visible, model.Visible),
            nameof(AccessNodeModel.Level) => Badges.Level(model.LevelValue, model.Level),
            nameof(AccessNodeModel.Origin) => Badges.Origin(model.OriginKind, model.Origin),
            nameof(AccessNodeModel.Warning) when !string.IsNullOrEmpty(model.Warning) =>
                Badges.WarningBadge(model.Warning),
            _ => null
        };
    }

    protected override ActionBase? GetListItemPrimaryAction(AccessNodeModel model)
    {
        if (model.PageId <= 0)
            return null; // website header row

        return NavigateScreenAction.To<PageAudienceScreen>()
            .With(new PageAudienceQuery
            {
                AccountKey = model.AccountKey,
                PageId = model.PageId
            });
    }
}
