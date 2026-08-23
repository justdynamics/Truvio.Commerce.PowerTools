using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Principals;
using Truvio.Commerce.PowerTools.Core.Principals.Dw;
using Truvio.Commerce.PowerTools.Core.Settings.Dw;
using Truvio.Commerce.PowerTools.Core.Settings;

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
    /// <summary>Default upper bound on users materialized per request; overridable in PowerTools settings.</summary>
    private const int DefaultUserFetchCap = 500;

    protected override IEnumerable<AccountListModel>? GetListItems()
    {
        var settings = DwPowerToolsSettings.Current;
        var userFetchCap = PowerToolsSettings.Positive(settings.UserFetchCap, DefaultUserFetchCap);
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

        var (users, totalUsers) = catalog.GetUsers(Search, userFetchCap);
        foreach (var user in users)
        {
            if (settings.HideAdministrators && user.BypassesChecks)
                continue;

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

        if (totalUsers > userFetchCap)
        {
            items.Add(new AccountListModel
            {
                AccountKey = string.Empty,
                Kind = "User",
                Name = $"... {totalUsers - userFetchCap} more users not shown",
                Detail = "Use the search to narrow the user list"
            });
        }

        return items;
    }

    protected override IEnumerable<AccountListModel> MapModels(IEnumerable<AccountListModel> items) => items;

    protected override DataListViewModel<AccountListModel> MakeListModel() => new();
}
