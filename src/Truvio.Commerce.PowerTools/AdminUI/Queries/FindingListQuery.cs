using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Diagnostics;
using Truvio.Commerce.PowerTools.Core.Permissions.Dw;
using Truvio.Commerce.PowerTools.Core.Rights.Dw;
using Truvio.Commerce.PowerTools.Core.Rights.Rules;
using Truvio.Commerce.PowerTools.Core.Settings.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>Runs every warning rule over the install and lists the findings.</summary>
public sealed class FindingListQuery : DataQueryListBase<FindingModel, FindingModel, DataListViewModel<FindingModel>>
{
    protected override IEnumerable<FindingModel>? GetListItems()
    {
        var findings = new WarningEngine().Run(new DwContentSecuritySource())
            .Concat(BackendRightsFindings())
            .ToList();

        // Suppression is never silent: whatever settings mute is counted in a trailing row.
        var filtered = DwPowerToolsSettings.Current.FilterWarningFindings(findings);

        var items = filtered.Visible.Select(f => new FindingModel
        {
            Severity = f.Severity.ToString(),
            RuleId = f.RuleId,
            Entity = f.EntityDisplayName,
            Title = f.Title,
            Detail = f.Detail
        }).ToList();

        if (filtered.HiddenCount > 0)
        {
            items.Add(new FindingModel
            {
                Severity = string.Empty,
                RuleId = string.Empty,
                Entity = "PowerTools settings",
                Title = filtered.HiddenNotice(),
                Detail = "Suppressed warning rules are configured under PowerTools ▸ Settings."
            });
        }

        return items;
    }

    /// <summary>
    /// The install-wide backend-rights findings (SECOPS-B*). They describe the admin's own gates
    /// rather than page permissions, but they are the same kind of latent misconfiguration, so they
    /// belong on the same screen. Only the rules that need no particular user are meaningful here —
    /// the per-user rule (SECOPS-B5) lives on the report itself.
    /// </summary>
    private static IEnumerable<Finding> BackendRightsFindings()
    {
        try
        {
            var snapshot = new DwRightsSource().Build(CurrentUserId());
            return new RightsWarningEngine().Run(snapshot)
                .Where(f => f.RuleId != NoVisibleAreaRule.Id);
        }
        catch
        {
            // The content findings must still list when the admin tree cannot be read.
            return [];
        }
    }

    private static int CurrentUserId()
    {
        try
        {
            return Dynamicweb.Security.UserManagement.UserContext.Current?.UserId ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    protected override IEnumerable<FindingModel> MapModels(IEnumerable<FindingModel> items) => items;

    protected override DataListViewModel<FindingModel> MakeListModel() => new();
}
