using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Principals.Dw;
using Truvio.Commerce.PowerTools.Core.Settings.Dw;
using Truvio.Commerce.PowerTools.Core.Settings;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// Account picker for the Price Explainer: the anonymous visitor plus users. Groups are not
/// offered because DW's price, assortment and discount engines all resolve from a concrete
/// user (its id, customer number and group memberships) — a bare group has no price context.
/// </summary>
public sealed class ExplainerAccountListQuery : DataQueryListBase<ExplainerAccountModel, ExplainerAccountModel, DataListViewModel<ExplainerAccountModel>>
{
    public const string AnonymousKey = "anonymous";

    private const int DefaultUserFetchCap = 500;

    protected override IEnumerable<ExplainerAccountModel>? GetListItems()
    {
        var settings = DwPowerToolsSettings.Current;
        var userFetchCap = PowerToolsSettings.Positive(settings.UserFetchCap, DefaultUserFetchCap);
        var catalog = new DwAccountCatalog();
        var items = new List<ExplainerAccountModel>
        {
            new()
            {
                AccountKey = AnonymousKey,
                Kind = "Visitor",
                Name = "Anonymous visitor",
                Detail = "Not signed in: only assortments allowing anonymous users and anonymous discounts apply"
            }
        };

        var (users, totalUsers) = catalog.GetUsers(Search, userFetchCap);
        foreach (var user in users)
        {
            if (settings.HideAdministrators && user.BypassesChecks)
                continue;

            var membership = $"Member of {user.OwnerIds.Count - 1} group(s)";
            items.Add(new ExplainerAccountModel
            {
                AccountKey = user.Id,
                Kind = "User",
                Name = user.DisplayName,
                Detail = string.IsNullOrEmpty(user.Email) ? membership : $"{membership} - {user.Email}"
            });
        }

        if (totalUsers > userFetchCap)
        {
            items.Add(new ExplainerAccountModel
            {
                AccountKey = string.Empty,
                Kind = "User",
                Name = $"... {totalUsers - userFetchCap} more users not shown",
                Detail = "Use the search to narrow the user list"
            });
        }

        return items;
    }

    protected override IEnumerable<ExplainerAccountModel> MapModels(IEnumerable<ExplainerAccountModel> items) => items;

    protected override DataListViewModel<ExplainerAccountModel> MakeListModel() => new();
}
