using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Diagnostics;
using Truvio.Commerce.PowerTools.Core.Permissions.Dw;
using Truvio.Commerce.PowerTools.Core.Settings.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>Runs every warning rule over the install and lists the findings.</summary>
public sealed class FindingListQuery : DataQueryListBase<FindingModel, FindingModel, DataListViewModel<FindingModel>>
{
    protected override IEnumerable<FindingModel>? GetListItems()
    {
        var findings = new WarningEngine().Run(new DwContentSecuritySource());

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

    protected override IEnumerable<FindingModel> MapModels(IEnumerable<FindingModel> items) => items;

    protected override DataListViewModel<FindingModel> MakeListModel() => new();
}
