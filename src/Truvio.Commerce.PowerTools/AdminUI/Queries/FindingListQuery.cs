using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Diagnostics;
using Truvio.Commerce.PowerTools.Core.Permissions.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>Runs every warning rule over the install and lists the findings.</summary>
public sealed class FindingListQuery : DataQueryListBase<FindingModel, FindingModel, DataListViewModel<FindingModel>>
{
    protected override IEnumerable<FindingModel>? GetListItems()
    {
        var findings = new WarningEngine().Run(new DwContentSecuritySource());

        var items = findings.Select(f => new FindingModel
        {
            Severity = f.Severity.ToString(),
            RuleId = f.RuleId,
            Entity = f.EntityDisplayName,
            Title = f.Title,
            Detail = f.Detail
        }).ToList();

        return items;
    }

    protected override IEnumerable<FindingModel> MapModels(IEnumerable<FindingModel> items) => items;

    protected override DataListViewModel<FindingModel> MakeListModel() => new();
}
