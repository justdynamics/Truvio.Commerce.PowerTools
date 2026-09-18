using System.Globalization;
using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Core;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Dw;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Models;
using Truvio.Commerce.PowerTools.Features.QueryTester.Core;
using Truvio.Commerce.PowerTools.Shared.AdminUI;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Queries;

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

        if (!string.IsNullOrWhiteSpace(Text))
        {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in document.Fields)
                fields[field.Name] = field.Value;

            var hits = QueryDiagnosis.TermHits(fields, [Text]);
            if (hits.Count == 0 && DocumentBrowserQuery.IsProductIndex(Repository, Item))
            {
                string Of(string name) => fields.TryGetValue(name, out var v) ? v : string.Empty;
                hits = QueryDiagnosis.TermHits(
                    DwIndexDocuments.ProductTexts(Of("ID"), Of("VariantID"), Of("LanguageID")), [Text]);
            }

            model.Sections.Add(new ReportSectionModel
            {
                Heading = $"Where '{Text}' matches",
                Html = hits.Count > 0
                    ? SearchTables.Table([],
                        hits.Select(h => new object?[]
                        {
                            h.Field,
                            new SearchTables.Snippets([(string.Empty, h.Before, h.Match, h.After)])
                        }))
                    : SearchTables.Note("The term does not occur in this document's stored fields or database texts - it matched via an analyzed-only field.")
            });
        }

        model.Sections.Add(new ReportSectionModel
        {
            Heading = $"Stored fields ({document.Fields.Count})",
            Html = SearchTables.Table(
                ["Field", "Value"],
                document.Fields.Select(f => new object?[]
                {
                    f.Name,
                    string.IsNullOrWhiteSpace(Text)
                        ? DocumentBrowserQuery.Trim(f.Value, 600)
                        : new SearchTables.Highlight(DocumentBrowserQuery.Trim(f.Value, 600), Text)
                }))
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
