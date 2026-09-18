using System.Globalization;
using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.Features.BackendRights.Dw;
using Truvio.Commerce.PowerTools.Features.BackendRights.Models;
using Truvio.Commerce.PowerTools.Features.Settings.Core;
using Truvio.Commerce.PowerTools.Features.Settings.Dw;

namespace Truvio.Commerce.PowerTools.Features.BackendRights.Queries;

/// <summary>
/// Step 1: pick a backend user. Only users who can actually reach the administration are listed
/// unless the toggle asks for the rest — an account with backend access off explains nothing.
/// </summary>
public sealed class BackendRightsListQuery : DataQueryListBase<BackendUserModel, BackendUserModel, DataListViewModel<BackendUserModel>>
{
    /// <summary>Also list users who cannot sign in to the administration.</summary>
    public bool ShowWithoutAccess { get; set; }

    protected override IEnumerable<BackendUserModel>? GetListItems()
    {
        var cap = PowerToolsSettings.Positive(DwPowerToolsSettings.Current.UserFetchCap, DwRightsSource.DefaultUserCap);
        var (users, total) = new DwRightsSource().GetBackendUsers(Search, ShowWithoutAccess, cap);

        var items = users.Select(u => new BackendUserModel
        {
            AccountKey = $"user:{u.UserId}",
            Name = $"{u.DisplayName} ({u.UserId})",
            UserName = u.UserName,
            BackendAccess = u.AllowBackend ? "Yes" : "No",
            Status = u.StatusName
        }).ToList();

        if (total > cap)
        {
            items.Add(new BackendUserModel
            {
                AccountKey = string.Empty,
                Name = $"... {total - cap} more users not shown",
                UserName = string.Empty,
                BackendAccess = string.Empty,
                Status = "Use the search to narrow the list"
            });
        }

        return items;
    }

    protected override IEnumerable<BackendUserModel> MapModels(IEnumerable<BackendUserModel> items) => items;

    protected override DataListViewModel<BackendUserModel> MakeListModel() => new();
}
