using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Principals;
using Truvio.Commerce.PowerTools.Core.Principals.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// The account picker: frontend roles, then user groups, then users. Deriving from
/// <see cref="DataQueryListBase{TModel, TDomainModel, TListModel}"/> gives the screen the
/// standard toolbar search, paging, and column sorting. The search text is additionally
/// pushed into DW's server-side user search, so large user stores are narrowed by
/// name/username/e-mail before the in-memory pass — searches by e-mail survive both filters
/// because the e-mail is part of the (searchable) Details column.
/// </summary>
public sealed class AccountListQuery : DataQueryListBase<AccountListModel, AccountListModel, DataListViewModel<AccountListModel>>
{
    /// <summary>Upper bound on users materialized per request; refine the search beyond it.</summary>
    private const int UserFetchCap = 500;

    protected override IEnumerable<AccountListModel>? GetListItems()
    {
        var catalog = new DwAccountCatalog();
        var items = new List<AccountListModel>();

        foreach (var role in catalog.GetRoles())
        {
            items.Add(new AccountListModel
            {
                AccountKey = role.Key,
                Kind = "Role",
                Name = role.DisplayName,
                Detail = "Built-in frontend role"
            });
        }

        foreach (var group in catalog.GetGroups())
        {
            items.Add(new AccountListModel
            {
                AccountKey = group.Key,
                Kind = "Group",
                Name = group.DisplayName,
                Detail = $"Group id {group.Id}"
            });
        }

        var (users, totalUsers) = catalog.GetUsers(Search, UserFetchCap);
        foreach (var user in users)
        {
            var membership = user.BypassesChecks
                ? "Administrator - sees everything, permissions are not evaluated"
                : $"Member of {user.OwnerIds.Count - 1} group(s)";
            items.Add(new AccountListModel
            {
                AccountKey = user.Key,
                Kind = "User",
                Name = user.DisplayName,
                Detail = string.IsNullOrEmpty(user.Email) ? membership : $"{membership} - {user.Email}",
                IsAdmin = user.BypassesChecks
            });
        }

        if (totalUsers > UserFetchCap)
        {
            items.Add(new AccountListModel
            {
                AccountKey = string.Empty,
                Kind = "User",
                Name = $"... {totalUsers - UserFetchCap} more users not shown",
                Detail = "Use the search to narrow the user list"
            });
        }

        return items;
    }

    protected override IEnumerable<AccountListModel> MapModels(IEnumerable<AccountListModel> items) => items;

    protected override DataListViewModel<AccountListModel> MakeListModel() => new();
}
