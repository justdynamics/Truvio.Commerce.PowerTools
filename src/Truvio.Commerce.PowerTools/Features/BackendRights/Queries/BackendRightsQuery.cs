using System.Globalization;
using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.Features.BackendRights.Dw;
using Truvio.Commerce.PowerTools.Features.BackendRights.Models;
using Truvio.Commerce.PowerTools.Shared.Principals;

namespace Truvio.Commerce.PowerTools.Features.BackendRights.Queries;

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
