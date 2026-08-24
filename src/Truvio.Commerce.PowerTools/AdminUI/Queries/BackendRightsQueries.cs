using System.Globalization;
using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Permissions;
using Truvio.Commerce.PowerTools.Core.Principals;
using Truvio.Commerce.PowerTools.Core.Rights;
using Truvio.Commerce.PowerTools.Core.Rights.Dw;
using Truvio.Commerce.PowerTools.Core.Rights.Rules;
using Truvio.Commerce.PowerTools.Core.Settings;
using Truvio.Commerce.PowerTools.Core.Settings.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

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

/// <summary>
/// Step 2: the report. Every gated thing in the admin tree for one user, with the gate that decided
/// and the evidence behind it.
/// </summary>
public sealed class BackendRightsQuery : DataQueryModelBase<BackendRightsModel>
{
    /// <summary>"user:17" — the same key shape the other Security screens round-trip.</summary>
    public string AccountKey { get; set; } = string.Empty;

    /// <summary>Show sections and nodes, not only the top-level areas.</summary>
    public bool ShowTree { get; set; } = true;

    // A method, not a property: every public property of a query is serialised into the screen URL.
    public int? GetUserId()
    {
        var parsed = SecurityAccount.ParseKey(AccountKey);
        if (parsed is null)
            return null;

        var (kind, id) = parsed.Value;
        return kind == SecurityAccountKind.User && int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId)
            ? userId
            : null;
    }

    public override BackendRightsModel? GetModel()
    {
        if (GetUserId() is not int userId)
            return new BackendRightsModel { Title = "Backend Rights", Error = "No backend user selected." };

        try
        {
            var snapshot = new DwRightsSource().Build(userId);
            return BackendRightsReport.Build(snapshot, ShowTree, AccountKey);
        }
        catch (Exception ex)
        {
            return new BackendRightsModel { Title = "Backend Rights", Error = ex.Message };
        }
    }
}

/// <summary>The "Why?" panel for one row of the report.</summary>
public sealed class BackendRightsWhyQuery : DataQueryModelBase<BackendRightsWhyModel>
{
    public string AccountKey { get; set; } = string.Empty;

    /// <summary>The <see cref="RightsNodeSpec.Id"/> to explain.</summary>
    public string NodeId { get; set; } = string.Empty;

    public override BackendRightsWhyModel? GetModel()
    {
        var parsed = SecurityAccount.ParseKey(AccountKey);
        if (parsed is null || parsed.Value.Kind != SecurityAccountKind.User
            || !int.TryParse(parsed.Value.Id, out var userId) || string.IsNullOrEmpty(NodeId))
        {
            return new BackendRightsWhyModel { Heading = "Why?", Html = SearchTables.Note("Nothing selected.") };
        }

        try
        {
            var snapshot = new DwRightsSource().Build(userId);
            var verdict = RightsEvaluator.Evaluate(snapshot)
                .FirstOrDefault(v => string.Equals(v.Node.Id, NodeId, StringComparison.OrdinalIgnoreCase));

            if (verdict is null)
                return new BackendRightsWhyModel { Heading = "Why?", Html = SearchTables.Note("That area is no longer installed.") };

            return new BackendRightsWhyModel
            {
                Heading = RightsExplanation.Headline(verdict),
                Html = BackendRightsReport.WhyHtml(snapshot, verdict)
            };
        }
        catch (Exception ex)
        {
            return new BackendRightsWhyModel { Heading = "Why?", Html = SearchTables.Note(ex.Message) };
        }
    }
}
