# Query tester

PowerTools ▸ **Search** ▸ *Query tester*. Read-only. Answers: *I ran this query — is the
result what I expect, if not why not, and what do I change?*

Requires Read on the `truvio-powertools-search` function grant (the same grant as the rest of
the Index & Query Inspector; see `docs/index-inspector.md`).

The linter (`docs/index-inspector.md`) reasons about a query *statically*. The tester runs it
against the live index and explains the result it actually got.

## The three screens

### 1. Query tester (the picker)

One row per repository query: repository, query, its source index, how many parameters it
declares and **how many of those carry a default**, and the health of the source index with
its online instance. `33 (0 with a default)` on a product query is the blank-parameter leak
waiting to happen — see IDX-W1/IDX-W2 in the inspector doc.

Picking a row runs the query with no values supplied.

### 2. Set parameters

A dialog opened from the report's *Set parameters* action: one text input per declared
parameter, pre-filled with the current run's values, each with its type, its declared default
and what happens when it stays blank as the field hint. A *Tester settings* group holds
**Expected document (#expect)** — the document key the "Why not X?" section explains, e.g. a
product ID. *Run the query* saves the set and re-runs the report; *Cancel* discards.

Leaving a field blank clears that value (the clause goes back to using its default, or to
vanishing). *Use the declared defaults* and *Clear all values* stay on the report's actions.

Under the hood the dialog's OK stores the typed set as a per-user draft on the server and
opens the report with `UseDraft=true` — an action URL is fixed at render time, so the values
themselves cannot travel in it. The report resolves the draft before it builds its own
actions, so **every link the report renders carries the resolved values explicitly**
(`Parameters=name=value;name2=value2`): anything you copy or click from the report is frozen
and shareable, and two people testing the same query never see each other's draft. Names may
contain spaces (`Bike type=Gravel`) and values may contain `=`; a value cannot contain `;`.
Names beginning with `#` are tester settings rather than query parameters and are never
handed to the index provider.

### 3. The report

An info bar (index, instance, hits, time, verdict) and then:

**Result** — three runs side by side, so the narrowing is visible:

| Row | Meaning |
| --- | --- |
| Every document in the index | the ceiling; a match-all probe on the online instance |
| This query, no values supplied | what a caller that passes nothing gets — declared defaults only |
| This run | with your values |

plus the parameter values used and **the executed Lucene query text** (see "DW facts" below).

**Documents** — the first N (≤ 25) hits: key, label, and **Matches** — search highlighting:
where the supplied values occur in each document, e.g.
`Long description (database): Owens Corning …EcoTouch®… insulation`. Stored index fields are
scanned first (free); when a value only matched analyzed-only fields (freetext aggregates
the un-stored descriptions), the product's database texts — name, number, descriptions,
meta, custom fields — are scanned instead, because that is where the match actually comes
from. Catch-all fields (freetext, `*_Search`) are hidden when a real field carries the term.
Each row links **Why?** (re-runs the report with `#expect=<key>`, so the probe section
explains that document clause by clause) and **Open** (the full stored document in the
Document browser).

**Clause trace** — every node of the expression tree in tree order, with the value it resolved
to for this run and a verdict:

| Verdict | Meaning |
| --- | --- |
| Active | reaches the index provider and constrains the result |
| Dropped | its value is null, so the provider removes it silently — the row says whether that widens (And group) or narrows (Or group) the result |
| Disabled | switched off in the query editor |
| Unknown field | its field is not in the index schema; on 10.21+ the provider logs a warning and drops the clause |
| Throws | same, but on 10.19 and older the provider throws and the whole query fails |
| Always true | nothing is left of the expression at all — the provider returns the entire index |

**Per-clause impact** — for each active clause (first 20), two extra real queries: the query
with that one clause switched off, and that clause on its own. A clause whose own hit count is
0 kills every result it is ANDed with, and that is called out.

**Why not 'X'?** — with `#expect=<key>` set: the document is fetched from the index by its key
field, and every active clause is re-run as `<keyField> = <key> AND <that clause>`. A failing
row is the reason the document is missing. The document's own value for the clause field is
shown next to it; a field that is indexed but not stored says so instead of showing nothing.

**Facet counts** — optional; the facet groups whose source is this query, counted for this
result set. A facet with no values at all is shown as such, because that is the finding.

**Suggestions** — the concrete edits, ordered Fix → Check → Note. Give a parameter a default,
correct a field name, use `Contains` instead of `Equal` on an analyzed field, rebuild a stale
index, and so on.

## The measurement methods, and their limits

- **Per-clause removal** flips `BinaryExpression.Disabled` on the clause after reloading the
  query, so the query the provider parses is the real one minus exactly that clause. It is not
  an emulation — the same provider, the same analyzer, the same schema.
- **Clause-alone** builds a `Query` whose expression IS that clause object, reusing it from the
  freshly loaded query.
- **The expectation probe** builds `Group(And, [ID == key, clause])` with
  `ExpressionHelper.CreateFieldExpression` and the clause object.
- Nothing is written back: `IQueryService.SaveQuery` is never called, and every run reloads the
  query from disk first (see below), so a mutated flag can never leak into another request.
- The impact measurement costs two queries per active clause, capped at
  `DwQueryRunner.MaxMeasuredClauses` (20). It can be switched off from the Impact toolbar selector.
- Group `Negate` and full-text expressions are traced but not individually measured.

## Dynamicweb facts verified for this tool

All read out of the shipped assemblies with `ilspycmd`; the package is `Dynamicweb.Core`
(namespace `Dynamicweb.Indexing.Querying`) and `Dynamicweb.Indexing.Lucene4`.

- **The provider hands back the executed Lucene query.** `QuerySettings.IncludeDebugInfo`
  makes `LuceneIndexProvider.SearchInternal` fill `IQueryResult.DebugInfo` with a
  `QueryDebugInfo { QueryText, ExecutionTime }`, where `QueryText` is
  `$"{value} TIMINGS {timings}"` — the parsed `Lucene.Net.Search.Query` followed by per-stage
  timings. The tester keeps the query and drops the timings.
- **A missing clause field changed behaviour between 10.19 and 10.21.**
  `Helpers.ParseQueryExpressionInternal` at 10.8.4 … 10.19.7 does
  `throw new ArgumentException("The given field name does not exist in the given index schema. …")`;
  at 10.21.13 and later the same place does
  `LogManager.System.GetLogger("Provider", "LuceneIndexProvider").Warn(<same message>); return null;`
  — the clause is dropped and the query still runs, wider than intended. The consequence is
  compiled in per host through the `DW_DROPS_UNKNOWN_CLAUSE_FIELD` constant in the csproj
  (`>= 10.21.0`), and passed into the pure diagnosis as `RunInputs.DropsUnknownField`. (10.20.x
  was not available locally, so 10.21 is the first version verified to warn.)
- **The order inside a clause is Disabled → field lookup → value.** A disabled binary
  expression returns null before anything is looked at; the field is looked up next; only then
  is the right-hand value resolved and `value == null && op != IsEmpty` drops the clause. The
  trace mirrors that order exactly, which is why an unknown field outranks a blank parameter.
- **An empty supplied value is NOT the same as no value.**
  `LuceneQueryProvider.HandleParameters` leaves a supplied empty string in the dictionary
  (`if (!string.IsNullOrEmpty(text)) parameters[name] = TypeParser.Parse(...)` — the empty case
  simply falls through), so `Helpers.GetValueFromExpression` returns `""` rather than null and
  the clause compares against an empty string instead of disappearing. The tester therefore
  omits empty values from `QuerySettings.Parameters`, which reproduces the frontend
  (`QueryHelper.ParseQueryParameters` skips values that `ValueConverter.ConvertString` turns
  into null).
- **Supplied values are parsed to the declared type.** `HandleParameters` runs
  `TypeParser.Parse(parameter.TypeName, text)` over a supplied string, so `System.String[]`
  and `System.Boolean[]` parameters accept plain text from the URL.
- **`QueryService.LoadQuery(repository, item)` caches nothing** — it is an `XDocument.Load` per
  call, followed by `QueryHelper.ConvertToQuery`. That is what makes "reload, disable one
  clause, run" safe.
- **The query's own sort order applies unless you replace it.**
  `LuceneQueryProvider.BuildSortOrder` starts with `sortOrder = sortOrder ?? query.SortOrder`,
  so leaving `QuerySettings.SortOrder` null runs the query's declared sorts.
- **Expression objects can be composed but not constructed directly.** Every
  `Expression`/`BinaryExpression`/`GroupExpression` constructor is `internal`; the public
  factories are `Expression.Binary/Group/Field/Parameter/Constant/Term/Macro/Code/FullTextSearch`
  and `ExpressionHelper.CreateFieldExpression(fieldName, fieldSystemName, value, OperatorType)`.
- **`OperatorType`** is `GreaterThan = 1, GreaterThanOrEqual, LessThan, LessThanOrEqual, Equal,
  MatchAny, MatchAll, Contains, ContainsExtended, In, Between, And, Or, IsEmpty` — `And = 12`,
  which is why `GroupExpression` maps to `Occur.MUST` only for `And` and to `Occur.SHOULD`
  otherwise.
- **Macros resolve through `MacroService.Evaluate`.** `MacroExpression.GetValue()` is public,
  so the tester can resolve a clause's macro in the current context and report what it
  produced. A macro that only resolves on the frontend (`Dynamicweb.Ecommerce.Context:ShopID`,
  `Dynamicweb.UserManagement.Context:FavoritesAutoIdByUserId`) returns nothing in the backend,
  which drops the clause — verified live on the harness.
- **Facets only come back when you ask for them.** `QuerySettings.Facets` takes
  `IEnumerable<IFacetGroup>`; `IQueryService.LoadFacets(repository, item)` loads one, and
  `IFacetGroup.Source` (`FacetSource { Repository, Item }`) is what links it to a query. The
  counts arrive as `IQueryResult.FacetGroupResult[groupName].GetFacetResults(facetName)`,
  a list of `FacetResult { Label, Value, Count }`.
- **`LuceneIndexReader.GetFieldTermsAndCount` is public but not safe for an admin screen.**
  It walks `MultiFields.GetFields(reader)` over *every* field and only stops once `resultLimit`
  matches for the requested field have been found, so a field that sorts late costs a full walk
  of every earlier field's term dictionary — and it opens `index.Instances.First().Key` rather
  than the online instance. The tester takes sample values from the returned documents instead.

## CoreUI facts verified for this tool

- **`DataQueryListBase.GetModel` builds its filter settings BEFORE calling the query**, then
  applies them to whatever `GetListItems()` returns — so a query cannot repurpose the toolbar
  `Search` text by clearing it inside `GetListItems`. Overriding
  `GetPreparedListItems(out int? totalCount)` and returning a list *plus* a total count takes
  the "already prepared" branch, which skips the base class's filtering, sorting and paging
  entirely. That is what lets *Set parameters* read `name=value` out of the search box.
- **A command cannot carry typed form values into a navigation.**
  `RunCommandAction.WithNavigateOnSuccess<TScreen, TQuery, TModel>(query, replace)` takes the
  query object as it was built when the screen was rendered, so an `EditScreenBase` "Set
  parameters" step could not hand its edited values to the tester URL. Hence the search-box
  input above.
- **`Icon.Flask`, `Icon.Play`, `Icon.SlidersV`, `Icon.Comparison`, `Icon.ChartBar`,
  `Icon.Redo`, `Icon.TrashAlt`** all exist at the 10.8.4 floor.
- **Every column but the last is `nowrap` in the search report tables**, which pushes a table
  wider than its card when a *leading* column holds long text (a clause like
  `ProductCategory|electronic_engine_system|battery_effect In @Battery_Effect`). `SearchTables`
  now has a `Wrap` cell marker for those columns.

## Code layout

- `Core/Search/Testing/ParameterValues.cs` — the `name=value;name2=value` URL syntax, the
  `#`-prefixed tester settings, and the "empty means omit" rule.
- `Core/Search/Testing/ClauseTrace.cs` — `ClauseVerdict`, `ValueOrigin`, `ClauseTrace`,
  `Suggestion`, `RunInputs`, `ClauseImpact`, `ExpectationCheck`.
- `Core/Search/Testing/QueryDiagnosis.cs` — pure: specs + values → trace + suggestions, and the
  measured/expectation suggestion sets.
- `Core/Search/Dw/DwQueryRunner.cs` — the live runs (whole query, one clause off, one clause
  alone, one clause against one key), macro resolution, and facet counts.
- `AdminUI/Queries/QueryTestQueries.cs`, `AdminUI/Screens/QueryTestScreens.cs`,
  `AdminUI/Models/QueryTestModels.cs`, `AdminUI/Tree/SearchTestingNavigationPaths.cs`.
- `tests/…/QueryDiagnosisTests.cs`, `tests/…/ParameterValuesTests.cs`.
