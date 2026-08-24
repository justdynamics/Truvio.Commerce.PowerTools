using System.Collections;
using System.Globalization;
using Dynamicweb.Indexing;
using Dynamicweb.Indexing.Queries;
using Dynamicweb.Indexing.Querying;
using Dynamicweb.Indexing.Querying.Expressions;
using DwQuery = Dynamicweb.Indexing.Querying.Query;

namespace Truvio.Commerce.PowerTools.Core.Search.Dw;

/// <summary>One field of an index document, already rendered for display.</summary>
public sealed record DocumentField(string Name, string Value);

/// <summary>How an indexed product document compares with the product row in the database.</summary>
public enum ProductMatch
{
    NotAProduct,
    Match,
    Differs,
    MissingInDatabase,
    Unknown
}

public sealed record DocumentDifference(string Field, string InIndex, string InDatabase);

public sealed record IndexDocumentRow(
    int Ordinal,
    string Key,
    IReadOnlyList<DocumentField> Fields,
    ProductMatch Match,
    IReadOnlyList<DocumentDifference> Differences)
{
    public string? Value(string field) =>
        Fields.FirstOrDefault(f => string.Equals(f.Name, field, StringComparison.OrdinalIgnoreCase))?.Value;
}

public sealed record DocumentBrowseResult(
    int TotalCount,
    IReadOnlyList<IndexDocumentRow> Documents,
    string Error)
{
    public static DocumentBrowseResult Failed(string error) => new(0, [], error);
}

/// <summary>
/// Read-only document browser: runs an ad-hoc query against a live index instance through
/// the platform's own <see cref="IndexQueryProvider"/> and, for product indexes, compares
/// each document with the product row the database currently holds.
/// <para>
/// A <see cref="DwQuery"/> with a null expression is deliberately allowed: the index provider
/// falls back to a match-all query, which is exactly "show me the first N documents".
/// </para>
/// </summary>
public static class DwIndexDocuments
{
    /// <summary>Hard result cap — this is a diagnostic, not a data export.</summary>
    public const int MaxTake = 50;

    /// <summary>
    /// The cap actually applied: the PowerTools "document rows" setting, itself capped at 500 so
    /// a mistyped value can never turn the browser into an export.
    /// </summary>
    public static int MaxTakeLimit() => Math.Clamp(
        Settings.PowerToolsSettings.Positive(Settings.Dw.DwPowerToolsSettings.Current.DocumentRowsPerPage, MaxTake), 1, 500);

    // The system names the product index builder actually emits (ProductIndexSchemaExtender
    // maps ProductID -> "ID", ProductNumber -> "Number", ProductName -> "Name", ...).
    private const string ProductIdField = "ID";
    private const string VariantIdField = "VariantID";
    private const string LanguageIdField = "LanguageID";
    private const string ProductKeyField = "ProductKey";

    /// <summary>Fields shown first in the document table, when the index has them.</summary>
    private static readonly string[] PreferredFields =
    [
        ProductIdField, VariantIdField, LanguageIdField,
        "Number", "Name", "Active", "Price", "Updated",
        "PageID", "PageName", "AreaID", "Title", "UserID", "UserName", "Email"
    ];

    public static DocumentBrowseResult Browse(
        string repository,
        string item,
        string freeText,
        string fieldName,
        string fieldValue,
        int take,
        bool compareWithDatabase)
    {
        if (string.IsNullOrEmpty(repository) || string.IsNullOrEmpty(item))
            return DocumentBrowseResult.Failed("No index selected.");

        take = Math.Clamp(take <= 0 ? 10 : take, 1, MaxTakeLimit());

        IIndex? index;
        try
        {
            index = Dynamicweb.Extensibility.ServiceLocator.Current.GetInstance<IIndexService>()
                .LoadIndex(repository, item);
        }
        catch (Exception ex)
        {
            return DocumentBrowseResult.Failed($"The index could not be loaded: {ex.Message}");
        }

        if (index is null)
            return DocumentBrowseResult.Failed("The index could not be loaded.");

        Expression? expression;
        try
        {
            expression = BuildExpression(freeText, fieldName, fieldValue);
        }
        catch (Exception ex)
        {
            return DocumentBrowseResult.Failed($"The search could not be built: {ex.Message}");
        }

        var query = new DwQuery
        {
            Name = "Truvio PowerTools document browser",
            Meta = new Dictionary<string, string>(),
            Settings = new Dictionary<string, string>(),
            Parameters = [],
            Imports = [],
            References = [],
            SortOrder = [],
            Source = new QuerySource { Repository = repository, Item = item },
            Expression = expression
        };

        IQueryResult? result;
        try
        {
            result = new IndexQueryProvider().Query(query, new QuerySettings
            {
                Take = take,
                Skip = 0,
                Parameters = new Dictionary<string, object>()
            });
        }
        catch (Exception ex)
        {
            return DocumentBrowseResult.Failed($"The query failed: {ex.Message}");
        }

        if (result is null)
            return DocumentBrowseResult.Failed(
                "The online index instance is not available — build the index before browsing its documents.");

        var documents = new List<IndexDocumentRow>();
        var ordinal = 0;
        foreach (var entry in result.QueryResult ?? [])
        {
            if (entry is not IDictionary<string, object> document)
                continue;

            ordinal++;
            var fields = Order(document, index).ToList();
            var (match, differences) = compareWithDatabase
                ? CompareWithProduct(document)
                : (ProductMatch.Unknown, (IReadOnlyList<DocumentDifference>)[]);

            documents.Add(new IndexDocumentRow(
                ordinal,
                Key(document, ordinal),
                fields,
                match,
                differences));
        }

        return new DocumentBrowseResult(result.TotalCount, documents, string.Empty);
    }

    /// <summary>
    /// Does this index carry product documents (and so support the database comparison)?
    /// "ProductKey" is unique to the product index schema, so it is a safer marker than "ID".
    /// </summary>
    public static bool IsProductIndex(IndexSpec index) =>
        index.Field(ProductKeyField) is not null ||
        index.BuilderType.Contains("Product", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// How many documents the online instance currently holds — a match-all query with Take=1
    /// reports the true hit count in <see cref="IQueryResult.TotalCount"/>. Null when the
    /// instance is not available (never built, or mid-build).
    /// </summary>
    public static int? Count(string repository, string item)
    {
        try
        {
            var result = new IndexQueryProvider().Query(
                new DwQuery
                {
                    Name = "Truvio PowerTools document count",
                    Meta = new Dictionary<string, string>(),
                    Settings = new Dictionary<string, string>(),
                    Parameters = [],
                    Imports = [],
                    References = [],
                    SortOrder = [],
                    Source = new QuerySource { Repository = repository, Item = item }
                },
                new QuerySettings { Take = 1, Skip = 0, Parameters = new Dictionary<string, object>() });

            return result?.TotalCount;
        }
        catch
        {
            return null;
        }
    }

    private static Expression? BuildExpression(string freeText, string fieldName, string fieldValue)
    {
        if (!string.IsNullOrWhiteSpace(fieldName) && !string.IsNullOrWhiteSpace(fieldValue))
        {
            return ExpressionHelper.CreateFieldExpression(
                fieldName, fieldName, fieldValue, OperatorType.Equal);
        }

        if (!string.IsNullOrWhiteSpace(freeText))
        {
            // Null fields = search every field in the schema (the provider fills them in).
            return Expression.FullTextSearch(null!, freeText.Trim(), FullTextSearchWildcardTypes.WildCardTrailing);
        }

        // No expression at all: the provider falls back to match-all, i.e. the first N documents.
        return null;
    }

    private static IEnumerable<DocumentField> Order(IDictionary<string, object> document, IIndex index)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var preferred in PreferredFields)
        {
            if (document.TryGetValue(preferred, out var value) && seen.Add(preferred))
                yield return new DocumentField(preferred, Render(value));
        }

        // Then the rest, in schema order when the schema is readable, otherwise document order.
        IEnumerable<string> rest;
        try
        {
            var schema = (index.Schema?.Fields ?? [])
                .Select(f => f.SystemName)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();
            rest = schema.Where(document.ContainsKey).Concat(document.Keys).Distinct(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            rest = document.Keys;
        }

        foreach (var name in rest)
        {
            if (!seen.Add(name))
                continue;
            if (document.TryGetValue(name, out var value))
                yield return new DocumentField(name, Render(value));
        }
    }

    private static string Key(IDictionary<string, object> document, int ordinal)
    {
        if (document.TryGetValue(ProductIdField, out var productId) && productId is not null)
        {
            var variant = document.TryGetValue(VariantIdField, out var v) ? Render(v) : string.Empty;
            return string.IsNullOrEmpty(variant) ? Render(productId) : $"{Render(productId)}/{variant}";
        }

        return $"#{ordinal}";
    }

    internal static string Render(object? value)
    {
        switch (value)
        {
            case null:
                return string.Empty;
            case string text:
                return text;
            case bool flag:
                return flag ? "True" : "False";
            case DateTime date:
                return date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            case double or float or decimal:
                return Convert.ToDouble(value, CultureInfo.InvariantCulture).ToString("0.####", CultureInfo.InvariantCulture);
            case IEnumerable list:
            {
                var parts = list.Cast<object?>().Take(20).Select(Render).ToList();
                return string.Join(", ", parts);
            }
            default:
                return value.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// The database-side texts of one product, for search highlighting: a term often lives
    /// only in analyzed index fields (freetext aggregates the descriptions un-stored), so the
    /// index document cannot show WHERE it matched — the product row can. Field names carry a
    /// "(database)" suffix so the report says where the text was read from.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ProductTexts(string productId, string variantId, string languageId)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(productId))
            return result;

        Dynamicweb.Ecommerce.Products.Product? product;
        try
        {
            product = string.IsNullOrEmpty(languageId)
                ? Dynamicweb.Ecommerce.Services.Products.GetProductById(productId, variantId, true)
                : Dynamicweb.Ecommerce.Services.Products.GetProductById(productId, variantId, languageId);
        }
        catch
        {
            return result;
        }

        if (product is null)
            return result;

        Add("Name", product.Name);
        Add("Number", product.Number);
        Add("Short description", product.ShortDescription);
        Add("Long description", product.LongDescription);
        Add("Meta title", product.Meta.Title);
        Add("Meta keywords", product.Meta.Keywords);

        try
        {
            foreach (var fieldValue in product.ProductFieldValues ?? [])
            {
                var name = fieldValue?.ProductField?.Name;
                var text = fieldValue?.Value?.ToString();
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(text))
                    Add(name!, text);
            }
        }
        catch
        {
            // Custom fields are a bonus - the built-in texts above still stand.
        }

        return result;

        void Add(string field, string? text)
        {
            var stripped = StripTags(text);
            if (!string.IsNullOrEmpty(stripped))
                result[$"{field} (database)"] = stripped;
        }
    }

    private static string StripTags(string? text) =>
        string.IsNullOrEmpty(text) ? string.Empty : System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", " ");

    /// <summary>
    /// Compares an indexed product document with the live product row. The index is a
    /// snapshot: any difference here means the document is stale and the index needs rebuilding.
    /// </summary>
    private static (ProductMatch, IReadOnlyList<DocumentDifference>) CompareWithProduct(IDictionary<string, object> document)
    {
        if (!document.ContainsKey(ProductKeyField))
            return (ProductMatch.NotAProduct, []);

        if (!document.TryGetValue(ProductIdField, out var rawId) || string.IsNullOrEmpty(Render(rawId)))
            return (ProductMatch.NotAProduct, []);

        var productId = Render(rawId);
        var variantId = document.TryGetValue(VariantIdField, out var v) ? Render(v) : string.Empty;
        var languageId = document.TryGetValue(LanguageIdField, out var l) ? Render(l) : string.Empty;

        Dynamicweb.Ecommerce.Products.Product? product;
        try
        {
            product = string.IsNullOrEmpty(languageId)
                ? Dynamicweb.Ecommerce.Services.Products.GetProductById(productId, variantId, true)
                : Dynamicweb.Ecommerce.Services.Products.GetProductById(productId, variantId, languageId);
        }
        catch
        {
            return (ProductMatch.Unknown, []);
        }

        if (product is null)
            return (ProductMatch.MissingInDatabase, []);

        var differences = new List<DocumentDifference>();

        Compare("Number", product.Number);
        Compare("Name", product.Name);
        Compare("Active", product.Active ? "True" : "False");
        Compare("Updated", product.Updated.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));

        void Compare(string field, string? databaseValue)
        {
            if (!document.TryGetValue(field, out var indexed))
                return;

            var left = Render(indexed).Trim();
            var right = (databaseValue ?? string.Empty).Trim();
            if (!string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
                differences.Add(new DocumentDifference(field, left, right));
        }

        return differences.Count == 0
            ? (ProductMatch.Match, [])
            : (ProductMatch.Differs, differences);
    }
}
