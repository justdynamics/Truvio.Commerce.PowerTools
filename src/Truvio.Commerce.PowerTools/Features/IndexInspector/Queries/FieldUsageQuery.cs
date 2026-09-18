using System.Globalization;
using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Core;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Models;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Queries;

/// <summary>Every field of every index, with the queries, sorts and facets that name it.</summary>
public sealed class FieldUsageQuery : DataQueryListBase<FieldUsageModel, FieldUsageModel, DataListViewModel<FieldUsageModel>>
{
    /// <summary>Only rows that need attention: dangling references and never-used fields.</summary>
    public bool ProblemsOnly { get; set; }

    protected override IEnumerable<FieldUsageModel>? GetListItems()
    {
        var usages = FieldUsageMap.Build(SearchQueryHelpers.Catalog())
            .Where(u => !ProblemsOnly || u.Dangling || u.Dead)
            .Where(u => SearchQueryHelpers.Matches(Search, u.FieldName, u.IndexName, u.Repository));

        // A dangling reference is a bug; an unused field is only housekeeping — and a large
        // content index produces thousands of those, so the bugs go first.
        if (ProblemsOnly)
            usages = usages.OrderByDescending(u => u.Dangling);

        return usages
            .Select(u => new FieldUsageModel
            {
                StatusKind = u.Status,
                Field = u.FieldName,
                // The same index name lives in several repositories, so qualify it.
                Index = $"{u.Repository}/{u.IndexName}",
                Type = Describe(u.Field),
                UsedBy = Describe(u),
                Status = u.Status
            })
            .ToList();
    }

    /// <summary>"String - stored, indexed": the field's type plus how it is written to the index.</summary>
    private static string Describe(IndexFieldSpec? field)
    {
        if (field is null)
            return "-";

        var flags = new List<string>(3);
        if (field.Stored)
            flags.Add("stored");
        if (field.Indexed)
            flags.Add("indexed");
        if (field.Analyzed)
            flags.Add("analyzed");

        return flags.Count == 0 ? field.ShortTypeName : $"{field.ShortTypeName} - {string.Join(", ", flags)}";
    }

    private static string Describe(FieldUsage usage)
    {
        var owners = usage.References
            .Select(r => r.Owner)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();

        if (owners.Count == 0)
            return usage.UsageSummary();

        var more = usage.References.Select(r => r.Owner).Distinct(StringComparer.OrdinalIgnoreCase).Count() - owners.Count;
        var suffix = more > 0 ? $" +{more}" : string.Empty;
        return $"{usage.UsageSummary()} ({string.Join(", ", owners)}{suffix})";
    }

    protected override IEnumerable<FieldUsageModel> MapModels(IEnumerable<FieldUsageModel> items) => items;

    protected override DataListViewModel<FieldUsageModel> MakeListModel() => new();
}
