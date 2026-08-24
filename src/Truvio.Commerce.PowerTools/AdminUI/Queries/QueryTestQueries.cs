using System.Globalization;
using Dynamicweb.CoreUI.Data;
using Dynamicweb.CoreUI.Data.DynamicFields;
using Dynamicweb.CoreUI.Editors;
using Dynamicweb.CoreUI.Editors.Inputs;
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
/// Builds the "Set parameters" dialog: one editable field per declared parameter (plus the
/// tester's own <c>#expect</c> setting), pre-filled from <see cref="Parameters"/>. A prompt
/// screen posts its edited model back to the OK command, so this is a real form — no toolbar
/// search box tricks. The field set differs per query, hence dynamic fields rather than a
/// fixed model shape.
/// </summary>
public sealed class QueryValuesQuery : DataQueryModelBase<QueryValuesModel>
{
    public string Repository { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    /// <summary>The values to pre-fill, as <c>name=value;name2=value2</c>.</summary>
    public string Parameters { get; set; } = string.Empty;

    public override QueryValuesModel? GetModel()
    {
        var model = new QueryValuesModel { Repository = Repository, Item = Item };
        if (string.IsNullOrEmpty(Repository) || string.IsNullOrEmpty(Item))
            return model;

        try
        {
            model.QueryName = SearchQueryHelpers.Catalog().Query(Repository, Item)?.Name ?? string.Empty;
        }
        catch
        {
            return model;
        }

        model.Fields = BuildFields(Repository, Item, Parameters);
        return model;
    }

    /// <summary>Also called by <see cref="QueryValuesModel.FillDynamicFields"/> when the OK command's posted model is rebuilt.</summary>
    internal static FieldGroupCollection BuildFields(string repository, string item, string parameters)
    {
        var fields = new FieldGroupCollection();
        if (string.IsNullOrEmpty(repository) || string.IsNullOrEmpty(item))
            return fields;

        QuerySpec? query;
        try
        {
            query = SearchQueryHelpers.Catalog().Query(repository, item);
        }
        catch
        {
            return fields;
        }

        if (query is null)
            return fields;

        var provider = new QueryValuesFieldProvider();
        var values = ParameterValues.Parse(parameters);

        var parameterFields = new List<Field>();
        foreach (var parameter in query.Parameters)
        {
            values.TryGetValue(parameter.Name, out var supplied);
            var used = query.Clauses().Any(c =>
                c.ValueKind == ClauseValueKind.Parameter &&
                string.Equals(c.ParameterName, parameter.Name, StringComparison.OrdinalIgnoreCase));

            parameterFields.Add(new Field(parameter)
            {
                Name = parameter.Name,
                SystemName = parameter.Name,
                TypeName = "System.String",
                Value = supplied ?? string.Empty,
                DefaultValue = parameter.HasDefault ? parameter.DefaultValue : string.Empty,
                Hint = Hint(parameter, used)
            });
        }

        var expect = ParameterValues.Reserved(parameters, ParameterValues.ExpectKeyName);
        var expectField = new Field(query)
        {
            Name = "Expected document (#expect)",
            SystemName = ParameterValues.ExpectKeyName,
            TypeName = "System.String",
            Value = expect,
            Hint = "The document key the \"Why not X?\" section explains, e.g. a product ID."
        };

        provider.AddGroup("Parameter values", parameterFields);
        provider.AddGroup("Tester settings", [expectField]);
        return provider.Collection;
    }

    private static string Hint(QueryParameterSpec parameter, bool used)
    {
        var type = IndexFieldSpec.ShortenType(parameter.TypeName);
        var tail = !used
            ? "No clause reads this parameter - it can only drive a facet."
            : parameter.HasDefault
                ? $"Blank runs the clause with the declared default ({parameter.DefaultValue})."
                : "Blank makes its clause DISAPPEAR - nothing constrains that field.";
        return $"{type}. {tail}";
    }
}

/// <summary>
/// Renders every parameter as a plain text input. A <see cref="FieldGroup"/> demands a
/// provider because DW's dynamic-field pipeline asks it for each field's editor; this one has
/// no persistence side (the OK command reads the posted values itself), so SaveChanges is a
/// no-op.
/// </summary>
internal sealed class QueryValuesFieldProvider : FieldEditorProviderBase
{
    private readonly List<FieldGroup> groups = [];
    private readonly FieldGroupCollection collection = new();

    public override FieldGroupCollection Collection => collection;

    public void AddGroup(string name, IEnumerable<Field> fields)
    {
        groups.Add(new FieldGroup(this)
        {
            Name = name,
            SystemName = string.Empty,
            Fields = fields.ToList()
        });
        collection.Groups = groups;
    }

    protected override EditorBase? GetEditor(Field field) => new Text
    {
        Name = field.SystemName,
        Label = field.Name,
        Hint = field.Hint,
        Value = Convert.ToString(field.Value, CultureInfo.InvariantCulture),
        Readonly = field.Readonly
    };

    public override object? SaveChanges(FieldGroupCollection fieldGroupCollection) => null;
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

    /// <summary>
    /// Read the run's values from <see cref="ParameterDraftStore"/> instead of the URL. The
    /// "Set parameters" dialog's OK command saves the typed values there and then navigates
    /// here with this flag: an action URL is fixed at render time, so the values themselves
    /// cannot travel in it. The screen mutates <see cref="Parameters"/> to the resolved set
    /// before actions are built, so every link this report renders is frozen and shareable.
    /// </summary>
    public bool UseDraft { get; set; }

    public override QueryTestModel? GetModel()
    {
        if (string.IsNullOrEmpty(Repository) || string.IsNullOrEmpty(Item))
            return new QueryTestModel { Error = "No query selected." };

        if (UseDraft && string.IsNullOrEmpty(Parameters))
            Parameters = ParameterDraftStore.Get(Repository, Item);

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
            model.Sections.Add(Results(run, query, index, values, traces));
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
            var (section, expectationSuggestions) = Expectation(expect);
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

    private ReportSectionModel Results(
        QueryRunResult run,
        QuerySpec query,
        IndexSpec? index,
        IReadOnlyDictionary<string, string> values,
        IReadOnlyList<ClauseTrace> traces)
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

        var keyField = DwQueryRunner.KeyFieldFor(index);

        // The bare key-field value per document ("Key" may be a compound like ID/VariantID).
        string KeyOf(RunDocument d) => d.Value(keyField) is { Length: > 0 } v ? v : d.Key;

        // Search highlighting, not clause mechanics: where do the supplied values occur in
        // each document? Costs nothing - the stored fields are already in the result. The
        // clause-by-clause view stays one click away behind "Why?".
        var showMatches = values.Count > 0;
        var isProductIndex = index is not null && DwIndexDocuments.IsProductIndex(index);

        var rows = run.Documents.Select(d =>
        {
            var key = KeyOf(d);
            var cells = new List<object?>
            {
                d.Ordinal.ToString(CultureInfo.InvariantCulture),
                d.Key,
                d.Label
            };

            if (showMatches)
            {
                // Stored index fields first (free); when the term only lives in analyzed
                // fields (freetext aggregates the un-stored descriptions), fall back to the
                // product's database texts - that is where the match actually comes from.
                var hits = QueryDiagnosis.TermHits(d.AsDictionary(), values.Values);
                if (hits.Count == 0 && isProductIndex)
                {
                    hits = QueryDiagnosis.TermHits(
                        DwIndexDocuments.ProductTexts(
                            d.Value("ID") ?? string.Empty,
                            d.Value("VariantID") ?? string.Empty,
                            d.Value("LanguageID") ?? string.Empty),
                        values.Values);
                }

                cells.Add(hits.Count > 0
                    ? new SearchTables.Snippets(hits.Select(h => (h.Field, h.Before, h.Match, h.After)).ToList())
                    : new SearchTables.Wrap("(no text field carries the value - likely matched via an id, category or analyzed-only field)"));
            }

            cells.Add(new SearchTables.ActionLink("Why?", WhyHref(key), WhyAction(key)));
            cells.Add(new SearchTables.Link("Open", OpenHref(query, keyField, key)));
            return (IReadOnlyList<object?>)cells;
        });

        var headers = showMatches
            ? new[] { "#", "Key", "Label", "Matches", "", "" }
            : new[] { "#", "Key", "Label", "", "" };
        var html = SearchTables.Table(headers, rows);

        html += SearchTables.Note(
            (showMatches
                ? "\"Matches\" shows where your values occur in each document's stored fields, like search highlighting. "
                : string.Empty) +
            "\"Why?\" re-runs the report explaining that document clause by clause; \"Open\" shows the stored document.");

        return new ReportSectionModel
        {
            Heading = $"Documents (first {run.Documents.Count} of {run.TotalHits:N0})",
            Html = html
        };
    }

    /// <summary>
    /// Opens the "Why 'X'?" panel as a slide-over — the same JSON the platform's own action
    /// tag helper emits for <c>OpenSlideOverAction</c>, hand-rendered because this link lives
    /// inside an HtmlBlock. The href fallback (full navigation with #expect) remains.
    /// </summary>
    private string WhyAction(string key)
    {
        var action = new Dictionary<string, object?>
        {
            ["name"] = "OpenSlideOver",
            ["parameters"] = new Dictionary<string, object?>
            {
                ["ScreenTypeName"] = "QueryWhy",
                ["ScreenType"] = "slideOver",
                ["Query"] = new Dictionary<string, object?>
                {
                    ["Repository"] = Repository,
                    ["Item"] = Item,
                    ["Parameters"] = Parameters,
                    ["Key"] = key,
                    ["Type"] = "QueryWhy",
                    ["QueryContext"] = new Dictionary<string, object?> { ["screenTypeName"] = "QueryWhy" }
                },
                ["ForceReload"] = false,
                ["NavigateByPost"] = false
            }
        };

        return System.Text.Json.JsonSerializer.Serialize(action);
    }

    private string WhyHref(string key)
    {
        var parameters = ParameterValues.Set(Parameters, ParameterValues.ExpectKeyName, key);
        return "/Admin/UI/PowerTools/QueryTest" +
               $"?Repository={Uri.EscapeDataString(Repository)}&Item={Uri.EscapeDataString(Item)}" +
               $"&Parameters={Uri.EscapeDataString(parameters)}" +
               $"&Take={Take}&Impact={Impact}&ShowFacets={ShowFacets}" +
               "&Type=QueryTest&QueryContext=Dynamicweb.CoreUI.Data.DataQueryContext";
    }

    private static string OpenHref(QuerySpec query, string keyField, string key) =>
        "/Admin/UI/PowerTools/DocumentDetail" +
        $"?Repository={Uri.EscapeDataString(query.SourceRepository)}&Item={Uri.EscapeDataString(query.SourceItem)}" +
        $"&Field={Uri.EscapeDataString(keyField)}&Value={Uri.EscapeDataString(key)}" +
        "&Type=DocumentDetail&QueryContext=Dynamicweb.CoreUI.Data.DataQueryContext";

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

    private (ReportSectionModel Section, IReadOnlyList<Suggestion> Suggestions) Expectation(string expectedKey)
    {
        var why = WhyReport.Build(Repository, Item, Parameters, expectedKey);
        return (new ReportSectionModel { Heading = why.Heading, Html = why.Html }, why.Suggestions);
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


/// <summary>Feeds the "Why 'X'?" slide-over: one document of one query run, explained.</summary>
public sealed class QueryWhyQuery : DataQueryModelBase<QueryWhyModel>
{
    public string Repository { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    /// <summary>The run's values, as <c>name=value;...</c> — the panel probes with them.</summary>
    public string Parameters { get; set; } = string.Empty;

    /// <summary>The document key to explain.</summary>
    public string Key { get; set; } = string.Empty;

    public override QueryWhyModel? GetModel()
    {
        if (string.IsNullOrEmpty(Repository) || string.IsNullOrEmpty(Item) || string.IsNullOrEmpty(Key))
            return new QueryWhyModel { Heading = "Why?", Html = SearchTables.Note("No document selected.") };

        var why = WhyReport.Build(Repository, Item, Parameters, Key);
        return new QueryWhyModel { Heading = why.Heading, Html = why.Html };
    }
}
