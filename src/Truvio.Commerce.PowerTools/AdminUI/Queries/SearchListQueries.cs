using System.Globalization;
using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Search;
using Truvio.Commerce.PowerTools.Core.Search.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>Shared helpers for the Search-section queries.</summary>
internal static class SearchQueryHelpers
{
    public static SearchCatalog Catalog() => SearchCatalog.From(new DwSearchSource());

    public static string HealthText(IndexSpec index) => index.Health switch
    {
        IndexHealth.Ok => "OK",
        IndexHealth.Stale => "Stale",
        IndexHealth.NeverBuilt => "Never built",
        IndexHealth.Failed => "Failed",
        _ => index.Health.ToString()
    };

    public static string When(DateTime? value) =>
        value.HasValue ? value.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : "-";

    public static bool Matches(string? search, params string?[] haystack) =>
        string.IsNullOrWhiteSpace(search) ||
        haystack.Any(h => !string.IsNullOrEmpty(h) && h.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));
}

/// <summary>Every repository's indexes, with builder, size and build health.</summary>
public sealed class IndexListQuery : DataQueryListBase<IndexListModel, IndexListModel, DataListViewModel<IndexListModel>>
{
    protected override IEnumerable<IndexListModel>? GetListItems()
    {
        var catalog = SearchQueryHelpers.Catalog();

        return catalog.Indexes
            .Where(index => SearchQueryHelpers.Matches(Search, index.Name, index.Repository, index.BuilderType))
            .Select(index => new IndexListModel
            {
                RepositoryName = index.Repository,
                Item = index.Item,
                HealthKind = index.Health.ToString(),
                Repository = index.Repository,
                Index = index.Name,
                Builder = string.IsNullOrEmpty(index.BuilderType) ? "-" : index.BuilderType,
                Fields = index.Fields.Count.ToString(CultureInfo.InvariantCulture),
                LastBuild = SearchQueryHelpers.When(index.LastBuild),
                Status = SearchQueryHelpers.HealthText(index)
            })
            .ToList();
    }

    protected override IEnumerable<IndexListModel> MapModels(IEnumerable<IndexListModel> items) => items;

    protected override DataListViewModel<IndexListModel> MakeListModel() => new();
}

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

/// <summary>Runs every lint rule over the repositories and lists the findings.</summary>
public sealed class QueryLintQuery : DataQueryModelBase<DataListViewModel<QueryLintModel>>
{
    public override DataListViewModel<QueryLintModel>? GetModel()
    {
        var findings = new QueryLintEngine().Run(SearchQueryHelpers.Catalog());

        var items = findings.Select(f => new QueryLintModel
        {
            Severity = f.Severity.ToString(),
            RuleId = f.RuleId,
            Entity = f.EntityDisplayName,
            Title = f.Title,
            Detail = f.Detail
        }).ToList();

        return new DataListViewModel<QueryLintModel>
        {
            Data = items,
            TotalCount = items.Count
        };
    }
}

/// <summary>Index picker for the document browser; shows the live document count per index.</summary>
public sealed class IndexPickQuery : DataQueryListBase<IndexPickModel, IndexPickModel, DataListViewModel<IndexPickModel>>
{
    protected override IEnumerable<IndexPickModel>? GetListItems()
    {
        var catalog = SearchQueryHelpers.Catalog();

        return catalog.Indexes
            .Where(index => SearchQueryHelpers.Matches(Search, index.Name, index.Repository))
            .Select(index =>
            {
                var count = DwIndexDocuments.Count(index.Repository, index.Item);
                return new IndexPickModel
                {
                    RepositoryName = index.Repository,
                    Item = index.Item,
                    HealthKind = index.Health.ToString(),
                    Repository = index.Repository,
                    Index = index.Name,
                    Instance = string.IsNullOrEmpty(index.OnlineInstance) ? "-" : index.OnlineInstance,
                    Documents = count.HasValue ? count.Value.ToString("N0", CultureInfo.InvariantCulture) : "-",
                    Status = SearchQueryHelpers.HealthText(index)
                };
            })
            .ToList();
    }

    protected override IEnumerable<IndexPickModel> MapModels(IEnumerable<IndexPickModel> items) => items;

    protected override DataListViewModel<IndexPickModel> MakeListModel() => new();
}
