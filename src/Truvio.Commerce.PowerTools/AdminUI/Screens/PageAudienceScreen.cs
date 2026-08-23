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
/// What the selected account experiences on one page: the page verdict, then each grid row
/// and paragraph with visible/hidden and the winning grant or deny.
/// </summary>
public sealed class PageAudienceScreen : ListScreenBase<AudienceItemModel>
{
    private SecurityAccount? _account;
    private string? _pageName;

    private PageAudienceQuery? AudienceQuery => Query as PageAudienceQuery;

    private SecurityAccount? Account => _account ??=
        AudienceQuery?.AccountKey is { Length: > 0 } key
            ? new DwAccountCatalog().Resolve(key)
            : null;

    private string PageName => _pageName ??=
        AudienceQuery?.PageId is int pageId and > 0
            ? Dynamicweb.Content.Services.Pages.GetPage(pageId)?.GetDisplayName() ?? $"page {pageId}"
            : "page";

    protected override string GetScreenName() =>
        Account is null
            ? "Content Access Viewer - page audience"
            : $"Page '{PageName}' seen by {Account.DisplayName}";

#if DW_HAS_SCREEN_EXPLANATION
    protected override string? GetScreenExplanation() =>
        Account is null
            ? null
            : $"Every grid row and paragraph on '{PageName}' and whether '{Account.DisplayName}' sees it";
#endif

    protected override IEnumerable<ActionGroup>? GetScreenActions() =>
    [
        new()
        {
            Nodes =
            [
                new ActionNode
                {
                    Name = "Back to content access",
                    Icon = Icon.ArrowLeft,
                    Sort = 10,
                    NodeAction = NavigateScreenAction.To<AccessOverviewScreen>()
                        .With(new AccessOverviewQuery { AccountKey = AudienceQuery?.AccountKey ?? string.Empty })
                },
                new ActionNode
                {
                    Name = "Select another user",
                    Icon = Icon.UserCircle,
                    Sort = 20,
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
                CreateMapping(m => m.ItemType),
                CreateMapping(m => m.Name),
                CreateMapping(m => m.Visible),
                CreateMapping(m => m.Level),
                CreateMapping(m => m.Reason)
            ]
        }
    ];

    protected override Cell? GetCell(string propertyName, AudienceItemModel model) =>
        propertyName switch
        {
            nameof(AudienceItemModel.Visible) => Badges.Visible(model.VisibleState, model.Visible),
            nameof(AudienceItemModel.Level) => Badges.Level(model.LevelValue, model.Level),
            nameof(AudienceItemModel.Reason) => Badges.Origin(model.OriginKind, model.Reason),
            _ => null
        };
}
