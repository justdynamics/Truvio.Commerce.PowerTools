using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Core;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Models;
using Truvio.Commerce.PowerTools.Features.Settings.Dw;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Queries;

/// <summary>Runs every lint rule over the repositories and lists the findings.</summary>
public sealed class QueryLintQuery : DataQueryModelBase<DataListViewModel<QueryLintModel>>
{
    public override DataListViewModel<QueryLintModel>? GetModel()
    {
        var findings = new QueryLintEngine().Run(SearchQueryHelpers.Catalog());

        // Settings can mute rules, parameters and whole queries — never silently: the count of
        // what was dropped is appended as the last row.
        var filtered = DwPowerToolsSettings.Current.FilterSearchFindings(findings);

        var items = filtered.Visible.Select(f => new QueryLintModel
        {
            Severity = f.Severity.ToString(),
            RuleId = f.RuleId,
            Entity = f.EntityDisplayName,
            Title = f.Title,
            Detail = f.Detail
        }).ToList();

        if (filtered.HiddenCount > 0)
        {
            items.Add(new QueryLintModel
            {
                Severity = string.Empty,
                RuleId = string.Empty,
                Entity = "PowerTools settings",
                Title = filtered.HiddenNotice(),
                Detail = "Ignored rule ids, parameters and queries are configured under PowerTools ▸ Settings."
            });
        }

        return new DataListViewModel<QueryLintModel>
        {
            Data = items,
            TotalCount = items.Count
        };
    }
}
