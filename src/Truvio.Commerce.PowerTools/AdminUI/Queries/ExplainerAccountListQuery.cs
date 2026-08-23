using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Principals.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// Account picker for the Price Explainer: the anonymous visitor plus users. Groups are not
/// offered because DW's price, assortment and discount engines all resolve from a concrete
/// user (its id, customer number and group memberships) — a bare group has no price context.
/// </summary>
public sealed class ExplainerAccountListQuery : DataQueryListBase<ExplainerAccountModel, ExplainerAccountModel, DataListViewModel<ExplainerAccountModel>>
{
    public const string AnonymousKey = "anonymous";

    private const int UserFetchCap = 500;

    protected override IEnumerable<ExplainerAccountModel>? GetListItems()
    {
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

        var (users, totalUsers) = catalog.GetUsers(Search, UserFetchCap);
        foreach (var user in users)
        {
            var membership = $"Member of {user.OwnerIds.Count - 1} group(s)";
            items.Add(new ExplainerAccountModel
            {
                AccountKey = user.Id,
                Kind = "User",
                Name = user.DisplayName,
                Detail = string.IsNullOrEmpty(user.Email) ? membership : $"{membership} - {user.Email}"
            });
        }

        if (totalUsers > UserFetchCap)
        {
            items.Add(new ExplainerAccountModel
            {
                AccountKey = string.Empty,
                Kind = "User",
                Name = $"... {totalUsers - UserFetchCap} more users not shown",
                Detail = "Use the search to narrow the user list"
            });
        }

        return items;
    }

    protected override IEnumerable<ExplainerAccountModel> MapModels(IEnumerable<ExplainerAccountModel> items) => items;

    protected override DataListViewModel<ExplainerAccountModel> MakeListModel() => new();
}
