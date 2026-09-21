using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Dw;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Models;
using Truvio.Commerce.PowerTools.Features.QueryTester.Core;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Queries;

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

        var searching = !string.IsNullOrWhiteSpace(Search);
        var productIndex = IsProductIndex(Repository, Item);

        return result.Documents.Select(document => new DocumentRowModel
        {
            RepositoryName = Repository,
            Item = Item,
            Ordinal = document.Ordinal,
            MatchKind = document.Match.ToString(),
            Key = document.Key,
            Label = First(document, LabelFields),
            // While a search is active the summary column answers "where did it hit?"
            // instead of showing the generic field digest.
            Summary = searching ? FoundIn(document, Search!, productIndex) : Summarise(document),
            Match = compare ? MatchText(document.Match) : "-"
        }).ToList();
    }

    /// <summary>"Long description (database): …Owens Corning EcoTouch®…" — same scan as the query tester.</summary>
    private static string FoundIn(IndexDocumentRow document, string search, bool productIndex)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in document.Fields)
            fields[field.Name] = field.Value;

        var hits = QueryDiagnosis.TermHits(fields, [search]);
        if (hits.Count == 0 && productIndex)
        {
            string Of(string name) => fields.TryGetValue(name, out var v) ? v : string.Empty;
            hits = QueryDiagnosis.TermHits(
                DwIndexDocuments.ProductTexts(Of("ID"), Of("VariantID"), Of("LanguageID")), [search]);
        }

        if (hits.Count == 0)
            return "(matched via an analyzed-only field)";

        var hit = hits[0];
        return $"{hit.Field}: {hit.Before}{hit.Match}{hit.After}";
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
