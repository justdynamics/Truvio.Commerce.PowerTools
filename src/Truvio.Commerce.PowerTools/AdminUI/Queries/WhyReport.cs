using Truvio.Commerce.PowerTools.Core.Search;
using Truvio.Commerce.PowerTools.Core.Search.Dw;
using Truvio.Commerce.PowerTools.Core.Search.Testing;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// The "Why 'X'?" panel for one document of one query run: every active clause probed as a
/// real query (<c>key = X AND clause</c>) with a verdict, plus — the part a person actually
/// reads — WHERE the clause's value occurs in the document ("'EcoTouch' is in Long
/// description (database): …"), found the same way as the Documents table's Matches column.
/// Shared by the report's inline section (<c>#expect</c>) and the slide-over the Documents
/// rows open.
/// </summary>
internal static class WhyReport
{
    internal sealed record Result(string Heading, string Html, IReadOnlyList<Suggestion> Suggestions);

    public static Result Build(string repository, string item, string parameters, string expectedKey, bool compact = false)
    {
        SearchCatalog catalog;
        try
        {
            catalog = SearchQueryHelpers.Catalog();
        }
        catch (Exception ex)
        {
            return new Result($"Why '{expectedKey}'?", SearchTables.Note($"The repositories could not be read: {ex.Message}"), []);
        }

        var query = catalog.Query(repository, item);
        if (query is null)
            return new Result($"Why '{expectedKey}'?", SearchTables.Note($"Query '{repository}/{item}' was not found."), []);

        var index = catalog.IndexFor(query);
        var values = ParameterValues.Effective(parameters);
        var inputs = RunInputs.For(query, index, parameters, QueryTestQuery.DropsUnknownClauseField)
            .WithRuntimeValues(DwQueryRunner.ResolveRuntimeValues(repository, item));
        var traces = QueryDiagnosis.Trace(inputs);

        var keyField = DwQueryRunner.KeyFieldFor(index);
        var lookup = DwQueryRunner.FindByKey(repository, item, keyField, expectedKey);
        var document = lookup.Documents.FirstOrDefault();

        if (!lookup.Ok || document is null)
        {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var missing = QueryDiagnosis.SuggestFromExpectation(traces, expectedKey, false, [], fields);

            return new Result(
                $"Why not '{expectedKey}'?",
                SearchTables.Note(
                    lookup.Ok
                        ? $"No document with {keyField} = '{expectedKey}' exists in {index?.Key ?? "the source index"}. " +
                          "No clause can bring back a document the index does not hold."
                        : lookup.Error),
                missing);
        }

        var documentFields = document.AsDictionary();

        // Where a value occurs: stored fields first, the product's database texts as well —
        // freetext matches come from analyzed copies of texts the index never stores.
        var texts = new Dictionary<string, string>(documentFields, StringComparer.OrdinalIgnoreCase);
        if (index is not null && DwIndexDocuments.IsProductIndex(index))
        {
            foreach (var pair in DwIndexDocuments.ProductTexts(
                document.Value("ID") ?? string.Empty,
                document.Value("VariantID") ?? string.Empty,
                document.Value("LanguageID") ?? string.Empty))
            {
                texts.TryAdd(pair.Key, pair.Value);
            }
        }

        var checks = new List<ExpectationCheck>();
        foreach (var trace in traces.Where(t => t.IsMeasurable).Take(DwQueryRunner.MaxMeasuredClauses))
        {
            var probe = DwQueryRunner.RunClauseForKey(repository, item, values, trace.Path, keyField, expectedKey);
            var passes = probe.Ok && probe.TotalHits > 0;
            var known = documentFields.TryGetValue(trace.Field, out var value);
            var actual = known ? value! : Unavailable(index, trace.Field);

            // The occurrence note earns its place when the row cannot show the value itself
            // (analyzed-only fields) or the term lives in ANOTHER field - repeating the
            // visible value ('ENU' is in LanguageID: "ENU") is noise.
            var note = passes
                ? known && actual.Contains(trace.ResolvedValue, StringComparison.OrdinalIgnoreCase)
                    ? string.Empty
                    : Occurrence(trace, texts)
                : known ? Note(trace, actual) : Unknown(trace);

            checks.Add(new ExpectationCheck(trace.Path, trace.Label, trace.Field, actual, passes, note));
        }

        // A slide-over is narrow: the four-column nowrap table overflows it, so the compact
        // layout stacks each clause as a block whose lines are free to wrap.
        var html = compact ? StackedHtml(checks) : TableHtml(checks, keyField, expectedKey);

        html += SearchTables.Note(
            $"Each row is a real query: '{keyField} = {expectedKey}' ANDed with that one clause. " +
            "A failing MUST clause is the reason a document is missing; a failing row inside an " +
            "Or group is just an alternative this document does not need.");

        var suggestions = QueryDiagnosis.SuggestFromExpectation(traces, expectedKey, true, checks, documentFields);

        // Membership = the WHOLE query ANDed with the key, not "all checks pass": in an Or
        // group a member legitimately fails the alternatives it does not need.
        var membership = DwQueryRunner.RunClauseForKey(repository, item, values, "1", keyField, expectedKey);
        var inResult = membership.Ok && membership.TotalHits > 0;

        return new Result(inResult ? $"Why '{expectedKey}'?" : $"Why not '{expectedKey}'?", html, suggestions);
    }

    private static string TableHtml(IReadOnlyList<ExpectationCheck> checks, string keyField, string expectedKey) =>
        SearchTables.Table(
            [$"Clause ({keyField} = {expectedKey})", "Value on this document", "Verdict", "Where / why"],
            checks.Select(c => new object?[]
            {
                new SearchTables.Wrap(c.Label),
                QueryDiagnosis.Shorten(c.DocumentValue, 90),
                new SearchTables.Pill(c.Passes ? "Passes" : "Fails", c.Passes ? "ok" : "bad"),
                new SearchTables.Wrap(c.Note)
            }));

    private static string StackedHtml(IReadOnlyList<ExpectationCheck> checks)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("<div style=\"padding:0 1.5rem .75rem 1.5rem\">");
        foreach (var c in checks)
        {
            sb.Append("<div style=\"padding:8px 0;border-bottom:1px solid rgba(128,128,128,.18)\">");
            sb.Append("<div style=\"display:flex;gap:8px;align-items:baseline\">");
            sb.Append(SearchTables.PillHtml(c.Passes ? "Passes" : "Fails", c.Passes ? "ok" : "bad"));
            sb.Append($"<span style=\"font-weight:600;word-break:break-word\">{SearchTables.E(c.Label)}</span>");
            sb.Append("</div>");

            // The value line only earns its place when it says something the note does not.
            var value = QueryDiagnosis.Shorten(c.DocumentValue, 160);
            if (!string.IsNullOrEmpty(value) && !value.StartsWith('('))
                sb.Append($"<div style=\"opacity:.8;word-break:break-word\">Value: {SearchTables.E(value)}</div>");
            else if (!string.IsNullOrEmpty(value))
                sb.Append($"<div style=\"opacity:.55;font-size:.85em\">{SearchTables.E(value)}</div>");

            if (!string.IsNullOrEmpty(c.Note))
                sb.Append($"<div style=\"word-break:break-word\">{SearchTables.E(c.Note)}</div>");

            sb.Append("</div>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    /// <summary>"'EcoTouch' is in Long description (database): "…Owens Corning EcoTouch®…"".</summary>
    private static string Occurrence(ClauseTrace trace, IReadOnlyDictionary<string, string> texts)
    {
        var value = trace.ResolvedValue;
        if (string.IsNullOrEmpty(value) || value.Length < 2)
            return string.Empty;

        var hit = QueryDiagnosis.TermHits(texts, [value], 1).FirstOrDefault();
        if (hit is null)
            return string.Empty;

        return $"'{QueryDiagnosis.Shorten(value, 40)}' is in {hit.Field}: \"{hit.Before}{hit.Match}{hit.After}\"";
    }

    private static string Unavailable(IndexSpec? index, string field)
    {
        var definition = index?.Field(field);
        if (definition is null)
            return "(field is not in the schema)";

        if (!definition.Stored)
        {
            return definition.Analyzed
                ? "(indexed and analyzed, not stored - the value cannot be read back)"
                : "(indexed, not stored - the value cannot be read back)";
        }

        return "(not on this document)";
    }

    private static string Unknown(ClauseTrace trace) =>
        $"Expected {trace.Operator} '{QueryDiagnosis.Shorten(trace.ResolvedValue, 60)}'. " +
        "The value cannot be read back from the index, so compare it against the stored sibling field instead.";

    private static string Note(ClauseTrace trace, string actual)
    {
        if (string.Equals(actual, trace.ResolvedValue, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(actual, trace.ResolvedValue, StringComparison.Ordinal))
        {
            return "Same text, different case - the analyzed term does not match the value you passed.";
        }

        if (actual.Contains(trace.ResolvedValue, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(trace.ResolvedValue))
        {
            return $"The document's value contains '{trace.ResolvedValue}' but is not equal to it - use Contains rather than {trace.Operator}.";
        }

        return $"Expected {trace.Operator} '{QueryDiagnosis.Shorten(trace.ResolvedValue, 60)}'.";
    }
}
