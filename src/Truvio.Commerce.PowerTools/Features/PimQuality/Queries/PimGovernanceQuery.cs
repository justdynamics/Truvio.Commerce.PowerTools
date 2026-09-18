using System.Globalization;
using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.Features.PimQuality.Models;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Queries;

/// <summary>Completion rules and workflows with their assignments — the governance list.</summary>
public sealed class PimGovernanceQuery : DataQueryListBase<PimGovernanceModel, PimGovernanceModel, DataListViewModel<PimGovernanceModel>>
{
    protected override IEnumerable<PimGovernanceModel>? GetListItems()
    {
        var source = PimQueryHelpers.Source();
        var items = new List<PimGovernanceModel>();

        foreach (var rule in source.GetRules())
        {
            items.Add(new PimGovernanceModel
            {
                State = rule.IsDead ? "Dead" : "Assigned",
                Kind = "Completion rule",
                Name = rule.Name,
                Status = rule.IsDead ? "Dead" : "Assigned",
                AppliesTo = rule.Usages.Count == 0 ? "nothing" : string.Join("; ", rule.Usages.Take(3)) +
                    (rule.Usages.Count > 3 ? $" +{rule.Usages.Count - 3}" : string.Empty),
                Fields = rule.FieldSystemNames.Count == 0 ? "-" : string.Join(", ", rule.FieldSystemNames.Take(4)) +
                    (rule.FieldSystemNames.Count > 4 ? $" +{rule.FieldSystemNames.Count - 4}" : string.Empty)
            });
        }

        foreach (var workflow in source.GetWorkflows())
        {
            var scope = (workflow.UsedByGroups, workflow.UsedByProducts) switch
            {
                (true, true) => "product groups and products",
                (true, false) => "product groups",
                (false, true) => "products",
                _ => "nothing"
            };

            items.Add(new PimGovernanceModel
            {
                State = workflow.IsReferenced ? "Assigned" : "Dead",
                Kind = "Workflow",
                Name = workflow.Name,
                Status = workflow.IsReferenced ? "In use" : "Unused",
                AppliesTo = scope,
                Fields = "-"
            });
        }

        return items
            .Where(i => SearchMatches(i))
            .OrderBy(i => i.Kind, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(i => i.State == "Dead")
            .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private bool SearchMatches(PimGovernanceModel model) =>
        string.IsNullOrWhiteSpace(Search) ||
        new[] { model.Name, model.Kind, model.Status, model.AppliesTo, model.Fields }
            .Any(v => !string.IsNullOrEmpty(v) && v.Contains(Search.Trim(), StringComparison.OrdinalIgnoreCase));

    protected override IEnumerable<PimGovernanceModel> MapModels(IEnumerable<PimGovernanceModel> items) => items;

    protected override DataListViewModel<PimGovernanceModel> MakeListModel() => new();
}
