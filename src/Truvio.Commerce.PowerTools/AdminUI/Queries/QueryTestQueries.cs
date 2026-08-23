using System.Collections.Concurrent;
using System.Globalization;
using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Search;
using Truvio.Commerce.PowerTools.Core.Search.Dw;
using Truvio.Commerce.PowerTools.Core.Search.Testing;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>Every repository query, with its source index and how many parameters can go blank.</summary>
public sealed class QueryPickQuery : DataQueryListBase<QueryPickModel, QueryPickModel, DataListViewModel<QueryPickModel>>
{
    protected override IEnumerable<QueryPickModel>? GetListItems()
    {
        var catalog = SearchQueryHelpers.Catalog();

        return catalog.Queries
            .Where(query => SearchQueryHelpers.Matches(Search, query.Name, query.Repository, query.SourceItem))
            .Select(query =>
            {
                var index = catalog.IndexFor(query);
                var withDefault = query.Parameters.Count(p => p.HasDefault);

                return new QueryPickModel
                {
                    RepositoryName = query.Repository,
                    Item = query.Item,
                    HealthKind = index?.Health.ToString() ?? "Missing",
                    Repository = query.Repository,
                    Query = query.Name,
                    Source = index is null ? $"{query.SourceKey} (missing)" : index.Name,
                    Parameters = query.Parameters.Count == 0
                        ? "0"
                        : $"{query.Parameters.Count} ({withDefault} with a default)",
                    Status = index is null
                        ? "Source missing"
                        : $"{SearchQueryHelpers.HealthText(index)}{OnlineSuffix(index)}"
                };
            })
            .ToList();
    }

    private static string OnlineSuffix(IndexSpec index) =>
        string.IsNullOrEmpty(index.OnlineInstance) ? " - no online instance" : $" - {index.OnlineInstance}";

    protected override IEnumerable<QueryPickModel> MapModels(IEnumerable<QueryPickModel> items) => items;

    protected override DataListViewModel<QueryPickModel> MakeListModel() => new();
}

/// <summary>
/// The "Set parameters" step. An overview screen has no form input, so this list screen turns
/// its toolbar search box into the input: text containing '=' is read as
/// <c>name=value;name2=value2</c> and merged into the run's parameter set, anything else
/// filters the list. The merged set lives on <see cref="Parameters"/>, which round-trips
/// through the screen URL.
/// </summary>
public sealed class QueryParameterQuery : DataQueryListBase<QueryParameterModel, QueryParameterModel, DataListViewModel<QueryParameterModel>>
{
    public string Repository { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    /// <summary>The run's values, as <c>name=value;name2=value2</c>.</summary>
    public string Parameters { get; set; } = string.Empty;

    /// <summary>
    /// Implemented instead of <c>GetListItems</c> on purpose: when this override returns a
    /// list AND a total count, <c>DataQueryListBase.GetModel</c> takes the "already prepared"
    /// branch and skips its own filtering, sorting and paging. That is what lets the toolbar
    /// search box carry a <c>name=value</c> assignment — the base class would otherwise apply
    /// the same text as a text filter over the rows (it builds its filter settings BEFORE
    /// calling the query) and every row would disappear.
    /// </summary>
    protected override IEnumerable<QueryParameterModel>? GetPreparedListItems(out int? totalCount)
    {
        var rows = Rows();
        totalCount = rows.Count;
        return rows;
    }

    private List<QueryParameterModel> Rows()
    {
        if (string.IsNullOrEmpty(Repository) || string.IsNullOrEmpty(Item))
            return [];

        // The search box is the input, but the admin client rebuilds the screen URL from the
        // page URL plus the new Search text — it never learns the merged Parameters. So a
        // second assignment would arrive with the URL's (stale) Parameters and wipe the first.
        // The draft below bridges that gap: while the search box is in use, the base is the
        // last merged set for this user and query; any link navigation (every link on this
        // screen carries the merged set, and "Clear all" carries an empty one) resets it.
        var typed = Search ?? string.Empty;
        var draftKey = DraftKey();
        if (string.IsNullOrEmpty(typed))
        {
            Drafts[draftKey] = Parameters;
        }
        else
        {
            if (string.IsNullOrEmpty(Parameters) && Drafts.TryGetValue(draftKey, out var draft))
                Parameters = draft;

            if (typed.Contains('='))
            {
                Parameters = ParameterValues.Merge(Parameters, typed);
                typed = string.Empty;
            }

            Drafts[draftKey] = Parameters;
        }

        SearchCatalog catalog;
        try
        {
            catalog = SearchQueryHelpers.Catalog();
        }
        catch
        {
            return [];
        }

        var query = catalog.Query(Repository, Item);
        if (query is null)
            return [];

        var values = ParameterValues.Parse(Parameters);
        var rows = new List<QueryParameterModel>();

        var expect = ParameterValues.Reserved(Parameters, ParameterValues.ExpectKeyName);
        rows.Add(new QueryParameterModel
        {
            RepositoryName = Repository,
            Item = Item,
            ParameterName = ParameterValues.ExpectKeyName,
            StateKind = string.IsNullOrEmpty(expect) ? "none" : "set",
            Name = ParameterValues.ExpectKeyName,
            Type = "Tester setting",
            Default = "-",
            Value = string.IsNullOrEmpty(expect) ? "(not set)" : expect,
            Effect = "The document key the \"Why not X?\" section explains, e.g. a product ID."
        });

        foreach (var parameter in query.Parameters)
        {
            values.TryGetValue(parameter.Name, out var supplied);
            var hasValue = !string.IsNullOrEmpty(supplied);
            var used = query.Clauses().Any(c =>
                c.ValueKind == ClauseValueKind.Parameter &&
                string.Equals(c.ParameterName, parameter.Name, StringComparison.OrdinalIgnoreCase));

            rows.Add(new QueryParameterModel
            {
                RepositoryName = Repository,
                Item = Item,
                ParameterName = parameter.Name,
                StateKind = hasValue ? "set" : parameter.HasDefault ? "default" : "blank",
                Name = parameter.Name,
                Type = IndexFieldSpec.ShortenType(parameter.TypeName),
                Default = parameter.HasDefault ? parameter.DefaultValue : "(none)",
                Value = hasValue ? supplied! : parameter.HasDefault ? $"{parameter.DefaultValue} (default)" : "(blank)",
                Effect = Effect(hasValue, parameter, used)
            });
        }

        return rows
            .Where(row => SearchQueryHelpers.Matches(typed, row.Name, row.Type, row.Value))
            .ToList();
    }

    /// <summary>The last merged parameter set per backend user and query, see <see cref="Rows"/>.</summary>
    private static readonly ConcurrentDictionary<string, string> Drafts = new(StringComparer.Ordinal);

    private string DraftKey()
    {
        string user;
        try
        {
            user = Dynamicweb.Security.UserManagement.User.GetCurrentBackendUser()?.ID.ToString(CultureInfo.InvariantCulture) ?? "-";
        }
        catch
        {
            user = "-";
        }

        return $"{user}|{Repository}|{Item}";
    }

    private static string Effect(bool hasValue, QueryParameterSpec parameter, bool used)
    {
        if (!used)
            return "No clause reads this parameter - it can only drive a facet.";

        if (hasValue)
            return "Its clause is active for this run.";

        return parameter.HasDefault
            ? "Its clause runs with the declared default."
            : "Its clause DISAPPEARS from the query - nothing constrains that field.";
    }

    protected override IEnumerable<QueryParameterModel> MapModels(IEnumerable<QueryParameterModel> items) => items;

    protected override DataListViewModel<QueryParameterModel> MakeListModel() => new();
}

/// <summary>
/// Runs one repository query against its live index and explains the result: what came back,
/// what every clause did, what each clause costs, why an expected document is missing, and
/// what to change. Read-only — the query file is loaded, never saved.
/// </summary>
public sealed class QueryTestQuery : DataQueryModelBase<QueryTestModel>
{
    public string Repository { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    /// <summary>The run's values, as <c>name=value;name2=value2</c>; '#'-names are tester settings.</summary>
    public string Parameters { get; set; } = string.Empty;

    public int Take { get; set; } = 10;

    /// <summary>Re-run the query once per active clause to measure what that clause costs.</summary>
    public bool Impact { get; set; } = true;

    /// <summary>Ask the provider for the facet counts of the facet groups that read this query.</summary>
    public bool ShowFacets { get; set; }

    public override QueryTestModel? GetModel()
    {
        if (string.IsNullOrEmpty(Repository) || string.IsNullOrEmpty(Item))
            return new QueryTestModel { Error = "No query selected." };

        SearchCatalog catalog;
        try
        {
            catalog = SearchQueryHelpers.Catalog();
        }
        catch (Exception ex)
        {
            return new QueryTestModel { Error = $"The repositories could not be read: {ex.Message}" };
        }

        var query = catalog.Query(Repository, Item);
        if (query is null)
            return new QueryTestModel { Error = $"Query '{Repository}/{Item}' was not found." };

        var index = catalog.IndexFor(query);
        var values = ParameterValues.Effective(Parameters);
        var inputs = RunInputs.For(query, index, Parameters, DropsUnknownClauseField)
            .WithRuntimeValues(DwQueryRunner.ResolveRuntimeValues(Repository, Item));
        var traces = QueryDiagnosis.Trace(inputs);

        var run = DwQueryRunner.Run(Repository, Item, values, Take, null, ShowFacets);

        var model = new QueryTestModel
        {
            Title = query.Name,
            Repository = Repository,
            Item = Item,
            QueryName = query.Name,
            IndexName = index?.Name ?? $"{query.SourceKey} (missing)",
            Instance = string.IsNullOrEmpty(run.Instance) ? index?.OnlineInstance ?? "-" : run.Instance,
            Hits = run.Ok ? run.TotalHits.ToString("N0", CultureInfo.InvariantCulture) : "-",
            Took = run.Ok ? $"{run.ElapsedMs:0.#} ms" : "-"
        };

        var suggestions = new List<Suggestion>(QueryDiagnosis.Suggest(inputs, traces));

        (model.Verdict, model.VerdictKind) = Verdict(run, traces);

        model.Sections.Add(Summary(query, index, run, values, traces));

        if (!run.Ok)
        {
            model.Sections.Add(new ReportSectionModel
            {
                Heading = "The query did not run",
                Html = SearchTables.Note(run.Error)
            });
        }
        else
        {
            model.Sections.Add(Results(run));
        }

        model.Sections.Add(Trace(traces));

        var impacts = new List<ClauseImpact>();
        if (Impact && run.Ok)
        {
            impacts.AddRange(Measure(traces, values));
            model.Sections.Add(Impacts(impacts, run.TotalHits, traces));
            suggestions.AddRange(QueryDiagnosis.SuggestFromImpact(run.TotalHits, DefaultsOnly(), impacts));
        }

        var expect = ParameterValues.Reserved(Parameters, ParameterValues.ExpectKeyName);
        if (!string.IsNullOrEmpty(expect) && run.Ok)
        {
            var (section, expectationSuggestions) = Expectation(expect, index, traces, values);
            model.Sections.Add(section);
            suggestions.AddRange(expectationSuggestions);
        }

        if (ShowFacets)
            model.Sections.Add(Facets(run));

        model.Sections.Add(Suggestions(suggestions));

        return model;
    }

    // ---- sections ------------------------------------------------------------------------

    private ReportSectionModel Summary(
        QuerySpec query,
        IndexSpec? index,
        QueryRunResult run,
        IReadOnlyDictionary<string, string> values,
        IReadOnlyList<ClauseTrace> traces)
    {
        var total = index is null ? null : DwQueryRunner.IndexTotal(index.Repository, index.Item);
        var defaults = DefaultsOnly();

        var rows = new List<object?[]>
        {
            new object?[]
            {
                "Every document in the index",
                new SearchTables.Pill(Number(total), "info"),
                index is null ? "The source index does not exist." : $"{index.Key} - the ceiling any run can reach."
            },
            new object?[]
            {
                "This query, no values supplied",
                new SearchTables.Pill(Number(defaults), defaults == total ? "warn" : "info"),
                defaults == total && total is not null
                    ? "The query narrows nothing on its own - every clause depends on a value."
                    : "What a caller that passes nothing gets: declared defaults only."
            },
            new object?[]
            {
                $"This run ({(values.Count == 0 ? "no values" : $"{values.Count} value(s)")})",
                new SearchTables.Pill(run.Ok ? Number(run.TotalHits) : "failed", run.Ok ? "ok" : "bad"),
                run.Ok
                    ? $"Returned the first {run.Returned} of them in {run.ElapsedMs:0.#} ms."
                    : run.Error
            }
        };

        var html = SearchTables.Table(["Run", "Hits", "What it means"], rows);

        if (values.Count > 0)
        {
            html += SearchTables.Table(
                ["Parameter", "Value"],
                values.Select(v => new object?[] { v.Key, v.Value }));
        }

        var dropped = traces.Count(t => t.Verdict == ClauseVerdict.Dropped);
        var active = traces.Count(t => !t.IsGroup && t.Verdict == ClauseVerdict.Active);
        html += SearchTables.Note(
            $"{query.Name}: {active} clause(s) active, {dropped} dropped, " +
            $"{query.Parameters.Count} declared parameter(s) of which {query.Parameters.Count(p => p.HasDefault)} carry a default. " +
            (string.IsNullOrEmpty(run.LuceneQuery) ? string.Empty : "The executed Lucene query is shown below."));

        if (!string.IsNullOrEmpty(run.LuceneQuery))
        {
            html += "<div style=\"padding:0 1.5rem .75rem 1.5rem\"><pre style=\"white-space:pre-wrap;word-break:break-word;margin:0;" +
                    "padding:8px 10px;border-radius:4px;background:rgba(128,128,128,.12);font-size:.85em\">" +
                    SearchTables.E(run.LuceneQuery) + "</pre></div>";
        }

        return new ReportSectionModel { Heading = "Result", Html = html };
    }

    private static ReportSectionModel Results(QueryRunResult run)
    {
        if (run.Documents.Count == 0)
        {
            return new ReportSectionModel
            {
                Heading = "Documents",
                Html = SearchTables.Note(
                    "The query matched nothing. The clause trace and the per-clause impact below say which clause is responsible.")
            };
        }

        return new ReportSectionModel
        {
            Heading = $"Documents (first {run.Documents.Count} of {run.TotalHits:N0})",
            Html = SearchTables.Table(
                ["#", "Key", "Label"],
                run.Documents.Select(d => new object?[]
                {
                    d.Ordinal.ToString(CultureInfo.InvariantCulture),
                    d.Key,
                    d.Label
                }))
        };
    }

    private static ReportSectionModel Trace(IReadOnlyList<ClauseTrace> traces)
    {
        var rows = traces.Select(t => new object?[]
        {
            new SearchTables.Wrap(Indent(t)),
            t.IsGroup ? "-" : $"{Display(t.ResolvedValue)} ({Origin(t)})",
            new SearchTables.Pill(t.IsGroup ? string.Empty : t.VerdictText, t.VerdictKind),
            t.Explanation
        });

        return new ReportSectionModel
        {
            Heading = "Clause trace",
            Html = SearchTables.Table(["Clause", "Resolved value", "Verdict", "Why"], rows)
        };
    }

    private static ReportSectionModel Impacts(
        IReadOnlyList<ClauseImpact> impacts,
        int totalHits,
        IReadOnlyList<ClauseTrace> traces)
    {
        if (impacts.Count == 0)
        {
            return new ReportSectionModel
            {
                Heading = "Per-clause impact",
                Html = SearchTables.Note(
                    "No clause survives to the index, so there is nothing to measure - the provider runs a match-all query.")
            };
        }

        var measurable = traces.Count(t => t.IsMeasurable);
        var rows = impacts.Select(i =>
        {
            var without = i.WithoutClause;
            var narrowed = without.HasValue ? without.Value - totalHits : (int?)null;
            var kind = i.KillsResult ? "bad" : narrowed is 0 ? "muted" : "ok";
            var note = i.KillsResult
                ? "On its own this clause matches NOTHING - it kills every result."
                : narrowed is 0
                    ? "Removing it changes nothing for these values."
                    : $"Removing it would add {narrowed:N0} document(s).";

            return new object?[]
            {
                new SearchTables.Wrap(i.Label),
                Number(without),
                Number(i.ClauseAlone),
                new SearchTables.Pill(narrowed switch { null => "?", 0 => "0", _ => $"-{narrowed:N0}" }, kind),
                note
            };
        });

        var html = SearchTables.Table(
            ["Clause", "Hits without it", "Hits from it alone", "It removes", "Reading"],
            rows);

        if (measurable > impacts.Count)
        {
            html += SearchTables.Note(
                $"Only the first {impacts.Count} of {measurable} active clauses were measured - each measurement is two extra queries.");
        }

        html += SearchTables.Note(
            "Measured by re-running the real query with that one clause switched off (the provider's own Disabled flag), " +
            "and by running the clause on its own. Nothing is written back to the query file.");

        return new ReportSectionModel { Heading = "Per-clause impact", Html = html };
    }

    private (ReportSectionModel Section, IReadOnlyList<Suggestion> Suggestions) Expectation(
        string expectedKey,
        IndexSpec? index,
        IReadOnlyList<ClauseTrace> traces,
        IReadOnlyDictionary<string, string> values)
    {
        var keyField = DwQueryRunner.KeyFieldFor(index);
        var lookup = DwQueryRunner.FindByKey(Repository, Item, keyField, expectedKey);
        var document = lookup.Documents.FirstOrDefault();

        if (!lookup.Ok || document is null)
        {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var missing = QueryDiagnosis.SuggestFromExpectation(traces, expectedKey, false, [], fields);

            return (new ReportSectionModel
            {
                Heading = $"Why not '{expectedKey}'?",
                Html = SearchTables.Note(
                    lookup.Ok
                        ? $"No document with {keyField} = '{expectedKey}' exists in {index?.Key ?? "the source index"}. " +
                          "No clause can bring back a document the index does not hold."
                        : lookup.Error)
            }, missing);
        }

        var documentFields = document.AsDictionary();
        var checks = new List<ExpectationCheck>();

        foreach (var trace in traces.Where(t => t.IsMeasurable).Take(DwQueryRunner.MaxMeasuredClauses))
        {
            var probe = DwQueryRunner.RunClauseForKey(Repository, Item, values, trace.Path, keyField, expectedKey);
            var passes = probe.Ok && probe.TotalHits > 0;
            var known = documentFields.TryGetValue(trace.Field, out var value);
            var actual = known ? value! : Unavailable(index, trace.Field);

            checks.Add(new ExpectationCheck(
                trace.Path,
                trace.Label,
                trace.Field,
                actual,
                passes,
                passes ? string.Empty : known ? Note(trace, actual) : Unknown(trace)));
        }

        var rows = checks.Select(c => new object?[]
        {
            new SearchTables.Wrap(c.Label),
            QueryDiagnosis.Shorten(c.DocumentValue, 90),
            new SearchTables.Pill(c.Passes ? "Passes" : "Fails", c.Passes ? "ok" : "bad"),
            c.Note
        });

        var html = SearchTables.Table(
            [$"Clause ({keyField} = {expectedKey})", "Value on this document", "Verdict", "Note"],
            rows);

        html += SearchTables.Note(
            $"Each row is a real query: '{keyField} = {expectedKey}' ANDed with that one clause. " +
            "A failing row is the reason the document is missing from the result.");

        var suggestions = QueryDiagnosis.SuggestFromExpectation(traces, expectedKey, true, checks, documentFields);

        var section = new ReportSectionModel { Heading = $"Why not '{expectedKey}'?", Html = html };
        return (section, suggestions);
    }

    private static ReportSectionModel Facets(QueryRunResult run)
    {
        if (run.Facets.Count == 0)
        {
            return new ReportSectionModel
            {
                Heading = "Facet counts",
                Html = SearchTables.Note(
                    "No facet group in this repository lists this query as its source, so there is nothing to count.")
            };
        }

        var rows = run.Facets.SelectMany(f => f.Values.Count == 0
            ? [new object?[] { $"{f.Group} / {f.Facet}", f.Field, new SearchTables.Pill("no values", "warn"), "0" }]
            : f.Values.Select(v => new object?[]
            {
                $"{f.Group} / {f.Facet}",
                f.Field,
                v.Key,
                v.Value.ToString("N0", CultureInfo.InvariantCulture)
            }));

        var html = SearchTables.Table(["Facet group", "Field", "Value", "Documents"], rows);
        html += SearchTables.Note(
            "Counted for THIS result set. A facet with no values usually means its field is not indexed, " +
            "or no document in the result carries a value for it.");

        return new ReportSectionModel { Heading = "Facet counts", Html = html };
    }

    private static ReportSectionModel Suggestions(IReadOnlyList<Suggestion> suggestions)
    {
        var ordered = suggestions
            .GroupBy(s => s.Title, StringComparer.Ordinal)
            .Select(g => g.First())
            .OrderBy(s => s.Rank)
            .ToList();

        if (ordered.Count == 0)
        {
            return new ReportSectionModel
            {
                Heading = "Suggestions",
                Html = SearchTables.Note("Nothing to change - every clause behaves as written.")
            };
        }

        return new ReportSectionModel
        {
            Heading = $"Suggestions ({ordered.Count})",
            Html = SearchTables.Table(
                ["Priority", "What", "Why and what to change"],
                ordered.Select(s => new object?[]
                {
                    new SearchTables.Pill(Label(s.Kind), s.Kind == "fix" ? "bad" : s.Kind == "warn" ? "warn" : "info"),
                    new SearchTables.Wrap(s.Title),
                    s.Detail
                }))
        };
    }

    // ---- helpers -------------------------------------------------------------------------

    /// <summary>
    /// How THIS host reacts to a clause whose field is missing from the index schema. Up to
    /// 10.19 the provider throws and the whole query fails; from 10.21 it logs a warning and
    /// drops the clause. Compile-time, because the DLL is built against the host's own version.
    /// A method, not a property — every public property of a query is serialised into the URL.
    /// </summary>
    internal static bool DropsUnknownClauseField =>
#if DW_DROPS_UNKNOWN_CLAUSE_FIELD
        true;
#else
        false;
#endif

    // Private field, not a property: every public property of a query is serialised into the URL.
    private int? _defaultsOnly;
    private bool _defaultsOnlyMeasured;

    /// <summary>The query with nothing supplied — the baseline every caller gets for free.</summary>
    private int? DefaultsOnly()
    {
        if (_defaultsOnlyMeasured)
            return _defaultsOnly;

        var run = DwQueryRunner.Run(Repository, Item, new Dictionary<string, string>(), 1);
        _defaultsOnly = run.Ok ? run.TotalHits : null;
        _defaultsOnlyMeasured = true;
        return _defaultsOnly;
    }

    private IReadOnlyList<ClauseImpact> Measure(
        IReadOnlyList<ClauseTrace> traces,
        IReadOnlyDictionary<string, string> values)
    {
        var impacts = new List<ClauseImpact>();

        foreach (var trace in traces.Where(t => t.IsMeasurable).Take(DwQueryRunner.MaxMeasuredClauses))
        {
            var without = DwQueryRunner.Run(Repository, Item, values, 1, trace.Path);
            var alone = DwQueryRunner.RunClauseAlone(Repository, Item, values, trace.Path);

            impacts.Add(new ClauseImpact(
                trace.Path,
                trace.Label,
                without.Ok ? without.TotalHits : null,
                alone.Ok ? alone.TotalHits : null));
        }

        return impacts;
    }

    private static (string Text, string Kind) Verdict(QueryRunResult run, IReadOnlyList<ClauseTrace> traces)
    {
        if (!run.Ok)
            return ("Failed", "bad");

        if (QueryDiagnosis.Throws(traces))
            return ("Broken clause", "bad");

        if (traces.Any(t => t.Verdict == ClauseVerdict.UnknownField))
            return ("Unknown field", "bad");

        if (QueryDiagnosis.Collapses(traces))
            return ("Whole index", "bad");

        if (run.TotalHits == 0)
            return ("No hits", "warn");

        return traces.Any(t => t.Verdict == ClauseVerdict.Dropped) ? ("Clauses dropped", "warn") : ("OK", "ok");
    }

    /// <summary>
    /// Why a document does not carry a readable value for the field: a field that is indexed
    /// but not stored is searchable yet absent from every returned document, so the index
    /// itself cannot show what it holds.
    /// </summary>
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

    private static string Indent(ClauseTrace trace)
    {
        var prefix = string.Concat(Enumerable.Repeat("   ", Math.Min(trace.Depth, 6)));
        return $"{prefix}{trace.Path}  {trace.Label}";
    }

    private static string Origin(ClauseTrace trace) => trace.Origin switch
    {
        ValueOrigin.SuppliedValue => "your value",
        ValueOrigin.ParameterDefault => "declared default",
        ValueOrigin.MissingParameter => "parameter (no value)",
        ValueOrigin.UndeclaredParameter => "undeclared parameter",
        ValueOrigin.Constant => "constant",
        ValueOrigin.Term => "term",
        ValueOrigin.Macro => "macro",
        ValueOrigin.Code => "code provider",
        ValueOrigin.FullText => "free text",
        _ => "-"
    };

    private static string Display(string value) =>
        string.IsNullOrEmpty(value) ? "(nothing)" : QueryDiagnosis.Shorten(value, 60);

    private static string Label(string kind) => kind switch
    {
        "fix" => "Fix",
        "warn" => "Check",
        _ => "Note"
    };

    private static string Number(int? value) =>
        value.HasValue ? value.Value.ToString("N0", CultureInfo.InvariantCulture) : "?";
}
