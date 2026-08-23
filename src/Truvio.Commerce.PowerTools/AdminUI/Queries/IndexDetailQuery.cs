using System.Globalization;
using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Search;
using Truvio.Commerce.PowerTools.Core.Search.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// One index in full: instances and their build status, the schema, the builder settings, and
/// everything in the repositories that reads from it.
/// </summary>
public sealed class IndexDetailQuery : DataQueryModelBase<IndexDetailModel>
{
    public string Repository { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    public override IndexDetailModel? GetModel()
    {
        if (string.IsNullOrEmpty(Repository) || string.IsNullOrEmpty(Item))
            return new IndexDetailModel { Error = "No index selected." };

        SearchCatalog catalog;
        try
        {
            catalog = SearchQueryHelpers.Catalog();
        }
        catch (Exception ex)
        {
            return new IndexDetailModel { Error = $"The repositories could not be read: {ex.Message}" };
        }

        var index = catalog.Index(Repository, Item);
        if (index is null)
            return new IndexDetailModel { Error = $"Index '{Repository}/{Item}' was not found." };

        var count = DwIndexDocuments.Count(index.Repository, index.Item);

        var model = new IndexDetailModel
        {
            Title = index.Name,
            Repository = index.Repository,
            Item = index.Item,
            Builder = string.IsNullOrEmpty(index.BuilderType) ? "-" : index.BuilderType,
            Balancer = string.IsNullOrEmpty(index.Balancer) ? "-" : index.Balancer,
            Status = SearchQueryHelpers.HealthText(index),
            HealthKind = index.Health.ToString(),
            StatusDetail = index.HealthDetail,
            Documents = count.HasValue ? count.Value.ToString("N0", CultureInfo.InvariantCulture) : "-",
            FieldCount = index.Fields.Count.ToString(CultureInfo.InvariantCulture),
            IsProductIndex = DwIndexDocuments.IsProductIndex(index)
        };

        model.Sections.Add(new ReportSectionModel { Heading = "Instances", Html = Instances(index) });
        model.Sections.Add(new ReportSectionModel { Heading = "Read by", Html = ReadBy(catalog, index) });
        model.Sections.Add(new ReportSectionModel { Heading = "Builder settings", Html = Builds(index) });
        model.Sections.Add(new ReportSectionModel
        {
            Heading = $"Schema fields ({index.Fields.Count})",
            Html = Fields(catalog, index)
        });

        return model;
    }

    private static string Instances(IndexSpec index) =>
        SearchTables.Table(
            ["Instance", "Provider", "Online", "Available", "Last build", "Duration", "State"],
            index.Instances.Select(i => new object?[]
            {
                i.Name,
                i.ProviderType,
                i.IsOnline ? new SearchTables.Pill("Online", "ok") : SearchTables.Pill.None,
                i.IsAvailable ? "Yes" : "No",
                SearchQueryHelpers.When(i.LastBuild),
                i.Duration.HasValue ? Format(i.Duration.Value) : "-",
                new SearchTables.Pill(i.State, i.State switch
                {
                    "Completed" => "ok",
                    "Failed" => "bad",
                    "Running" => "info",
                    _ => "warn"
                })
            }));

    private static string Format(TimeSpan span) =>
        span.TotalSeconds < 1
            ? "<1s"
            : span.TotalMinutes < 1
                ? $"{span.TotalSeconds:0}s"
                : $"{(int)span.TotalMinutes}m {span.Seconds}s";

    private static string ReadBy(SearchCatalog catalog, IndexSpec index)
    {
        var rows = new List<object?[]>();

        foreach (var query in catalog.QueriesFor(index))
        {
            var noDefaults = query.Parameters.Count(p => !p.HasDefault);
            rows.Add(
            [
                "Query",
                query.Name,
                $"{query.Parameters.Count} parameter(s)" + (noDefaults > 0 ? $", {noDefaults} without a default" : string.Empty),
                LuceneSemantics.Collapses(query)
                    ? new SearchTables.Pill("Matches everything", "bad")
                    : SearchTables.Pill.None
            ]);
        }

        foreach (var group in catalog.FacetGroupsFor(index))
        {
            rows.Add(
            [
                "Facets",
                group.Name,
                $"{group.Facets.Count} facet(s) on {group.SourceItem}",
                SearchTables.Pill.None
            ]);
        }

        return rows.Count == 0
            ? SearchTables.Note("No query and no facet group in any repository reads from this index.")
            : SearchTables.Table(["Kind", "Name", "Detail", string.Empty], rows);
    }

    private static string Builds(IndexSpec index)
    {
        if (index.Builds.Count == 0)
            return SearchTables.Note("This index has no build definition.");

        var rows = new List<object?[]>();
        foreach (var build in index.Builds)
        {
            rows.Add([$"{build.Name}", new SearchTables.Pill(build.Action, "info"), build.BuilderType]);
            foreach (var setting in build.Settings)
                rows.Add([string.Empty, setting.Key, string.IsNullOrEmpty(setting.Value) ? "(empty)" : setting.Value]);
        }

        return SearchTables.Table(["Build", "Setting", "Value"], rows);
    }

    private static string Fields(SearchCatalog catalog, IndexSpec index)
    {
        var usage = FieldUsageMap.Build(catalog)
            .Where(u => SearchKeys.Same(SearchKeys.For(u.Repository, u.IndexItem), index.Key))
            .ToDictionary(u => u.FieldName, StringComparer.OrdinalIgnoreCase);

        var rows = index.Fields.Select(field =>
        {
            usage.TryGetValue(field.SystemName, out var use);
            var used = use?.UsageSummary() ?? "-";
            return new object?[]
            {
                field.SystemName,
                field.ShortTypeName,
                Flags(field),
                string.IsNullOrEmpty(field.Analyzer) ? "-" : field.Analyzer,
                string.IsNullOrEmpty(field.Boost) ? "-" : field.Boost,
                used == "-" && field.Indexed
                    ? new SearchTables.Pill("unused", "warn")
                    : new SearchTables.Pill(used == "-" ? string.Empty : used, "info")
            };
        });

        return SearchTables.Table(["Field", "Type", "Flags", "Analyzer", "Boost", "Used by"], rows);
    }

    private static string Flags(IndexFieldSpec field)
    {
        var flags = new List<string>(3);
        if (field.Stored)
            flags.Add("stored");
        if (field.Indexed)
            flags.Add("indexed");
        if (field.Analyzed)
            flags.Add("analyzed");
        return flags.Count == 0 ? "-" : string.Join(", ", flags);
    }
}
