using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerApps.SecOps.AdminUI.Models;
using Truvio.Commerce.PowerApps.SecOps.AdminUI.Queries;
using Truvio.Commerce.PowerApps.SecOps.AdminUI.Security;

namespace Truvio.Commerce.PowerApps.SecOps.AdminUI.Screens;

/// <summary>Pick the security account whose content access to inspect.</summary>
public sealed class AccountListScreen : ListScreenBase<AccountListModel>
{
    protected override string GetScreenName() => "Security Viewer - accounts";

    protected override IEnumerable<ListViewMapping> GetViewMappings() =>
    [
        new RowViewMapping
        {
            Columns =
            [
                CreateMapping(m => m.Kind),
                CreateMapping(m => m.Name),
                CreateMapping(m => m.Detail)
            ]
        }
    ];

    protected override Cell? GetCell(string propertyName, AccountListModel model) =>
        propertyName switch
        {
            nameof(AccountListModel.Kind) when !string.IsNullOrEmpty(model.AccountKey) =>
                SecOpsBadges.AccountKind(model.Kind),
            nameof(AccountListModel.Detail) when model.IsAdmin =>
                Cell.MakeCell(new Dynamicweb.CoreUI.Displays.Information.Badge
                {
                    Value = model.Detail,
                    BadgeType = Dynamicweb.CoreUI.Displays.Information.BadgeType.Warning
                }),
            _ => null
        };

    protected override ActionBase? GetListItemPrimaryAction(AccountListModel model)
    {
        if (!SecOpsAccess.CanUseSecurityViewer() || string.IsNullOrEmpty(model.AccountKey))
            return null;

        return NavigateScreenAction.To<AccessOverviewScreen>()
            .With(new AccessOverviewQuery { AccountKey = model.AccountKey });
    }
}
