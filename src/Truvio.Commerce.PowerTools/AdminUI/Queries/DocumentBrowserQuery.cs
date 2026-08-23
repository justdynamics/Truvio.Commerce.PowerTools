using System.Globalization;
using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Search;
using Truvio.Commerce.PowerTools.Core.Search.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// Reads the top N documents of one index instance, optionally narrowed by the toolbar search
/// (free text across the whole schema) or by a field = value lookup, and — for product
/// indexes — compares each document with the product row the database holds right now.
/// Read-only: nothing is written back to the index.
/// </summary>
public sealed class DocumentBrowserQuery : DataQueryListBase<DocumentRowModel, DocumentRowModel, DataListViewModel<DocumentRowModel>>
{
    /// <summary>Fields tried, in order, as the human-readable label of a document.</summary>
    private static readonly string[] LabelFields = ["Name", "Title", "PageName", "UserName", "Number"];

    /// <summary>Fields tried, in order, for the one-line summary column.</summary>
    private static readonly string[] SummaryFields = ["Number", "LanguageID", "Active", "Price", "Updated"];

    public string Repository { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    /// <summary>Field = value lookup; takes precedence over the toolbar search.</summary>
    public string Field { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public int Take { get; set; } = 10;

    /// <summary>Compare each product document with the database row (product indexes only).</summary>
    public bool Compare { get; set; } = true;

    /// <summary>Set once the model has been produced, so the screen can explain what it shows.</summary>
    public string Status { get; set; } = string.Empty;

    protected override IEnumerable<DocumentRowModel>? GetListItems()
    {
        if (string.IsNullOrEmpty(Repository) || string.IsNullOrEmpty(Item))
            return [];

        var compare = Compare && IsProductIndex(Repository, Item);
        var result = DwIndexDocuments.Browse(Repository, Item, Search ?? string.Empty, Field, Value, Take, compare);

        if (!string.IsNullOrEmpty(result.Error))
        {
            return
            [
                new DocumentRowModel
                {
                    RepositoryName = Repository,
                    Item = Item,
                    Ordinal = 0,
                    Key = "-",
                    Label = result.Error,
                    Summary = string.Empty,
                    Match = string.Empty
                }
            ];
        }

        return result.Documents.Select(document => new DocumentRowModel
        {
            RepositoryName = Repository,
            Item = Item,
            Ordinal = document.Ordinal,
            MatchKind = document.Match.ToString(),
            Key = document.Key,
            Label = First(document, LabelFields),
            Summary = Summarise(document),
            Match = compare ? MatchText(document.Match) : "-"
        }).ToList();
    }

    internal static bool IsProductIndex(string repository, string item)
    {
        try
        {
            var index = SearchQueryHelpers.Catalog().Index(repository, item);
            return index is not null && DwIndexDocuments.IsProductIndex(index);
        }
        catch
        {
            return false;
        }
    }

    private static string MatchText(ProductMatch match) => match switch
    {
        ProductMatch.Match => "Match",
        ProductMatch.Differs => "Differs",
        ProductMatch.MissingInDatabase => "Deleted",
        ProductMatch.NotAProduct => "-",
        _ => "?"
    };

    private static string First(IndexDocumentRow document, IReadOnlyList<string> candidates)
    {
        foreach (var candidate in candidates)
        {
            var value = document.Value(candidate);
            if (!string.IsNullOrWhiteSpace(value))
                return Trim(value, 80);
        }

        var fallback = document.Fields.FirstOrDefault(f => !string.IsNullOrWhiteSpace(f.Value));
        return fallback is null ? string.Empty : Trim(fallback.Value, 80);
    }

    private static string Summarise(IndexDocumentRow document)
    {
        var parts = new List<string>(SummaryFields.Length);
        foreach (var field in SummaryFields)
        {
            var value = document.Value(field);
            if (!string.IsNullOrWhiteSpace(value))
                parts.Add($"{field}: {Trim(value, 40)}");
        }

        return parts.Count > 0
            ? string.Join(" · ", parts)
            : $"{document.Fields.Count} field(s)";
    }

    internal static string Trim(string? value, int max)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value.Length <= max ? value : value[..max] + "...";
    }

    protected override IEnumerable<DocumentRowModel> MapModels(IEnumerable<DocumentRowModel> items) => items;

    protected override DataListViewModel<DocumentRowModel> MakeListModel() => new();
}

/// <summary>
/// One document in full: every field the index stores for it, plus — for product documents —
/// the fields where the index and the database disagree.
/// </summary>
public sealed class DocumentDetailQuery : DataQueryModelBase<DocumentDetailModel>
{
    public string Repository { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    /// <summary>The free-text search that produced the list, so the same document is found again.</summary>
    public string Text { get; set; } = string.Empty;

    public string Field { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    /// <summary>1-based position in the result the list showed.</summary>
    public int Ordinal { get; set; } = 1;

    public override DocumentDetailModel? GetModel()
    {
        if (string.IsNullOrEmpty(Repository) || string.IsNullOrEmpty(Item))
            return new DocumentDetailModel { Error = "No index selected." };

        SearchCatalog catalog;
        try
        {
            catalog = SearchQueryHelpers.Catalog();
        }
        catch (Exception ex)
        {
            return new DocumentDetailModel { Error = $"The repositories could not be read: {ex.Message}" };
        }

        var index = catalog.Index(Repository, Item);
        if (index is null)
            return new DocumentDetailModel { Error = $"Index '{Repository}/{Item}' was not found." };

        var compare = DwIndexDocuments.IsProductIndex(index);
        var take = Math.Clamp(Math.Max(Ordinal, 1), 1, DwIndexDocuments.MaxTakeLimit());
        var result = DwIndexDocuments.Browse(Repository, Item, Text, Field, Value, take, compare);

        if (!string.IsNullOrEmpty(result.Error))
            return new DocumentDetailModel { Error = result.Error, IndexName = index.Name };

        var document = result.Documents.FirstOrDefault(d => d.Ordinal == Ordinal) ?? result.Documents.LastOrDefault();
        if (document is null)
            return new DocumentDetailModel { Error = "The document is no longer in the index.", IndexName = index.Name };

        var model = new DocumentDetailModel
        {
            Title = document.Key,
            Repository = index.Repository,
            Item = index.Item,
            IndexName = index.Name,
            Instance = string.IsNullOrEmpty(index.OnlineInstance) ? "-" : index.OnlineInstance,
            Key = document.Key,
            Match = compare ? Describe(document.Match) : "Not a product index",
            MatchKind = document.Match.ToString(),
            FieldCount = document.Fields.Count.ToString(CultureInfo.InvariantCulture)
        };

        if (compare && document.Differences.Count > 0)
        {
            model.Sections.Add(new ReportSectionModel
            {
                Heading = "Differences from the database",
                Html = SearchTables.Table(
                    ["Field", "In the index", "In the database"],
                    document.Differences.Select(d => new object?[]
                    {
                        d.Field,
                        new SearchTables.Pill(DocumentBrowserQuery.Trim(d.InIndex, 120), "bad"),
                        DocumentBrowserQuery.Trim(d.InDatabase, 200)
                    }))
            });
        }
        else if (compare && document.Match == ProductMatch.MissingInDatabase)
        {
            model.Sections.Add(new ReportSectionModel
            {
                Heading = "Differences from the database",
                Html = SearchTables.Note(
                    "This document has no matching product in the database any more — the index is out of date.")
            });
        }

        model.Sections.Add(new ReportSectionModel
        {
            Heading = $"Stored fields ({document.Fields.Count})",
            Html = SearchTables.Table(
                ["Field", "Value"],
                document.Fields.Select(f => new object?[] { f.Name, DocumentBrowserQuery.Trim(f.Value, 600) }))
        });

        return model;
    }

    private static string Describe(ProductMatch match) => match switch
    {
        ProductMatch.Match => "Matches the database",
        ProductMatch.Differs => "Differs from the database",
        ProductMatch.MissingInDatabase => "No longer in the database",
        ProductMatch.NotAProduct => "Not a product document",
        _ => "Unknown"
    };
}
