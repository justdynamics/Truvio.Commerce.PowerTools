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
            : new DwAccountCatalog().Resolve(AccessOverviewQuery.DefaultAccountKey());

    protected override string GetScreenName() =>
        Account is null
            ? "Overview"
            : Account.DisplayName;

#if DW_HAS_SCREEN_EXPLANATION
    protected override string? GetScreenExplanation() =>
        Account is null ? null : $"What '{Account.DisplayName}' can see, page by page";
#endif

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


/// <summary>
/// The account and website switches in the top bar: the account as a searchable slide-over
/// picker (accounts can be thousands - a plain dropdown cannot search), the website as a
/// value-labelled selector. Every pick re-navigates, and the query resolves the pick token
/// before the screen renders, so all links carry the resolved AccountKey explicitly.
/// </summary>
public sealed class AccessOverviewToolbarInjector : ScreenInjector<AccessOverviewScreen>
{
    public override void OnAfter(AccessOverviewScreen screen, Dynamicweb.CoreUI.UiComponentBase content)
    {
        if (content is not Dynamicweb.CoreUI.Layout.ScreenLayout layout)
            return;

        if (screen.Query is not AccessOverviewQuery q)
            return;

        // The list data (which normally resolves a pick token) may not have run yet when the
        // layout is built - resolve here as well so the button always shows the account.
        var accountKey = q.AccountKey;
        if (string.IsNullOrEmpty(accountKey) && !string.IsNullOrEmpty(q.PickToken))
            accountKey = Queries.PickStore.Get(q.PickToken);

        if (string.IsNullOrEmpty(accountKey))
            accountKey = AccessOverviewQuery.DefaultAccountKey();

        if (string.IsNullOrEmpty(accountKey))
            return;

        var token = Guid.NewGuid().ToString("N");
        ToolbarSwitch.AddPicker(layout, Resolve(accountKey), Icon.UserCircle,
            new Selectors.AccountSelectorProvider(), token,
            NavigateScreenAction.To<AccessOverviewScreen>()
                .With(new AccessOverviewQuery { PickToken = token, AreaId = q.AreaId }));

        var areas = Areas();
        if (areas.Count > 1)
        {
            var current = areas.FirstOrDefault(a => a.Id == q.AreaId);
            ToolbarSwitch.Add(layout, q.AreaId == 0 ? "All websites" : current.Name ?? "All websites", Icon.Globe,
                new[] { (Id: 0, Name: "All websites") }.Concat(areas).Select(a =>
                    ToolbarSwitch.Option(a.Name, active: a.Id == q.AreaId,
                        NavigateScreenAction.To<AccessOverviewScreen>()
                            .With(new AccessOverviewQuery { AccountKey = accountKey, AreaId = a.Id }))));
        }
    }

    private static string Resolve(string key)
    {
        try
        {
            return new Truvio.Commerce.PowerTools.Core.Principals.Dw.DwAccountCatalog().Resolve(key)?.DisplayName ?? key;
        }
        catch
        {
            return key;
        }
    }

    private static IReadOnlyList<(int Id, string Name)> Areas()
    {
        try
        {
            return new Truvio.Commerce.PowerTools.Core.Permissions.Dw.DwContentSecuritySource()
                .GetAreas()
                .Select(a => (a.Id, a.Name))
                .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}
