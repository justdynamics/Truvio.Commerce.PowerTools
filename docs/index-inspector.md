# Index & Query Inspector

PowerTools ▸ **Search**. Read-only. Answers: *what is in my indexes and queries, what is
stale, and which query will misbehave?*

Requires Read on the `truvio-powertools-search` function grant (PowerTools permission
entity "Truvio PowerTools").

## The four screens

### Repositories & indexes

One row per index across every repository, with builder, field count, last build time and a
health badge. Health is derived from the build status of each instance:

| Badge | Meaning |
| --- | --- |
| OK | Every instance has a completed build, the newest is younger than 24 h |
| Stale | Newest build is older than 24 h, or some instance has never been built |
| Never built | No instance has a build history at all |
| Failed | At least one instance failed on its last build |

The 24 h threshold is the platform's own: `IndexHelper.GetIndexStatusInformation` flags an
index as a warning once `lastBuildTime < DateTime.Now.AddHours(-24)`.

That 24 h default, the linter's rule/parameter/query suppressions and the document row cap are
configurable under **PowerTools ▸ Settings** (see `docs/settings.md`); anything the linter hides is
counted in a trailing "N findings hidden by settings" row.

Clicking a row opens the **index detail**: instances (provider, online flag, availability,
last build, duration, state), everything in the repositories that reads from the index,
the builder settings, and the full schema with per-field flags, analyzer, boost and a
"used by" column that marks indexed-but-never-referenced fields.

### Field where-used

One row per field of every index, with the query clauses, sort orders and facets that name
it. Two states earn a badge:

- **Dangling** — a query, sort or facet references a field the index schema does not have.
- **Unused** — an indexed field that no query, sort or facet ever asks for (dead weight in
  every rebuild).

The toolbar search narrows by field, index or repository name; a toolbar selector next to
the Actions menu switches between "all fields" and "only dangling and unused".

### Query linter

Every rule below runs over every query, sort, facet group and index in the install.

| Rule | Severity | What it catches |
| --- | --- | --- |
| IDX-W1 | Warning | Clause compared against a parameter with no default value — the clause vanishes when the parameter is not supplied |
| IDX-W2 | Critical | Every clause can vanish, so the query returns the entire index |
| IDX-W3 | Critical | Clause references a parameter the query does not declare |
| IDX-W4 | Info | Declared parameter that no clause and no facet uses |
| IDX-W5 | Info | Clause left disabled in the query editor |
| IDX-W6 | Critical | Query source points at an index that does not exist |
| IDX-W7 | Critical | Clause field is not in the source index schema (the provider throws) |
| IDX-W8 | Warning | Sort field is not in the schema — the sort is silently ignored |
| IDX-W9 | Warning | Sort field is stored but not indexed, so it carries no sortable values |
| IDX-W10 | Warning | Facet group's source query does not exist |
| IDX-W11 | Warning | Facet field is not in the index schema — the facet is skipped |
| IDX-W12 | Warning | Facet field is not indexed, so no facet values are ever written |
| IDX-W13 | Info | Facet on an analyzed field — buckets are analyzer tokens, not whole values |
| IDX-W14 | Warning | Facet has no query parameter, or one the source query does not declare |
| IDX-W15 | Info | Two queries with identical source, expression and sort order |
| IDX-W16 | Info | Index no query reads from |
| IDX-W17 | Critical / Warning | Index never built, last build failed, or older than 24 h |

### Document browser

Pick an index, then read what it actually contains. The toolbar search runs as a live
free-text query against every schema field — including analyzed catch-alls like `freetext`,
so a term that only lives in an un-stored description ("EcoTouch") still hits. While a
search is active, the Summary column says where it hit ("Long description (database):
…EcoTouch…"), and picking a row shows every stored field of that document plus a
"Where '<term>' matches" section with the occurrence highlighted. Results are capped at 50
(`DwIndexDocuments.MaxTake`).

For **product indexes** each listed document is compared with the product row the database
holds right now — `Number`, `Name`, `Active` and `Updated` — and the row is badged
*Match*, *Differs* or *Deleted*. A difference means the index is a stale snapshot and needs
rebuilding. The document detail screen lists the exact fields that disagree.

## The rules behind IDX-W1 and IDX-W2 (the blank-parameter leak)

This is the finding the tool exists for, and the chain is worth stating exactly. All of it
was read out of the shipped assemblies at the 10.8.4 floor.

1. `LuceneQueryProvider.HandleParameters(IQuery, IDictionary<string, object>)` seeds a
   parameter value **only** when `!parameters.ContainsKey(parameter.Name) &&
   !string.IsNullOrEmpty(parameter.DefaultValue)`. A parameter with an empty
   `DefaultValue` that no caller supplies never enters the dictionary.
2. The frontend path agrees: `QueryHelper.ParseQueryParameters` falls back to
   `ValueConverter.ConvertString(item.DefaultValue, type)`, and `ConvertString` returns
   `null` for a null-or-empty string, so the parameter is not added to the dictionary
   either.
3. `Helpers.GetValueFromExpression(Expression, IDictionary<string, object>)` returns `null`
   for a `ParameterExpression` whose `VariableName` is absent from that dictionary.
4. `Helpers.ParseQueryExpressionInternal` then drops the whole clause:
   `if (value == null && op != OperatorType.IsEmpty) return null;` — no error, no log.
5. A `GroupExpression` only adds non-null children, and yields `null` itself when nothing
   is left (`return clauses.Any() ? booleanQuery : null`). Collapse propagates upward.
6. `LuceneIndexProvider` finishes with
   `ParseQueryExpression(index, query.Expression, settings) ?? new MatchAllDocsQuery()`.

So a query whose clauses are all parameter-driven, none of them with a default, returns
**every document in the index** the moment nobody passes a parameter. IDX-W2 reports that
whole-query case as Critical; IDX-W1 reports the per-clause case, and says which direction
the result set moves: dropping a clause inside an `And` group removes a `MUST` and widens
the result, inside an `Or` group it removes a `SHOULD` and narrows it (`Occur` is chosen as
`operator != OperatorType.And ? SHOULD : MUST`).

`OperatorType.IsEmpty` is the single exception — the provider substitutes a value for it
rather than dropping the clause — so IDX-W1 leaves `IsEmpty` clauses alone.

## Other DW facts the rules rely on

- **Missing clause field throws.** `ParseQueryExpressionInternal` looks the field up in
  `index.Schema.Fields` and throws `ArgumentException("The given field name does not exist
  in the given index schema. …")`. Hence IDX-W7 is Critical while the sort and facet
  equivalents are only warnings.
- **Missing sort field is silent.** `LuceneQueryProvider.BuildSortOrder` does `continue`
  when `fields.FirstOrDefault(f => f.SystemName == sortInfo.Field)` is null, and
  `SearchInternal` falls back to `Sort.RELEVANCE` when the whole sort is empty.
- **Facet values only exist for indexed fields.** The writer calls `AddFacetFields(...)`
  guarded by `if (fieldDefinition.Indexed)`, and it emits one
  `SortedSetDocValuesFacetField` per analyzer token when `fieldDefinition.Analyzed` is set,
  otherwise one per whole value. That is IDX-W12 and IDX-W13.
- **A facet on an unknown field is skipped.** `DoFacetSearch` only fills a facet when
  `fields.FirstOrDefault(f => f.SystemName == facet.Field)` is non-null and
  `facet.QueryParameter != null` (IDX-W11, IDX-W14).
- **Repository layout.** `RepositoryService` walks the folders under
  `RepositoryService.BaseFolder` (`/Files/System/Repositories`); the built-in providers
  report `*.index` as `TypeName = "Index"`, `*.query` as `"Query"` and `*.facets` as
  `"Facets"`. `IIndexService.LoadIndex`, `IQueryService.LoadQuery` and
  `IQueryService.LoadFacets` parse them.
- **Extender fields are part of the schema.** `IndexHelper.FillIndexWithSchema` unions the
  fields the `ExtensionFieldDefinition` produces into `SchemaDefinition.Fields`, while
  `SchemaDefinition.FieldsFromIndexDefinition` keeps the raw `<Extension>` entry. The
  inspector reads `Fields`, so a schema declared as a single `<Extension>` element (the
  usual product index) still shows its ~90 real fields.
- **Product index system names are not the database column names.**
  `ProductIndexSchemaExtender` maps `ProductID → "ID"`, `ProductNumber → "Number"`,
  `ProductName → "Name"`, `ProductActive → "Active"`, `ProductLanguageID → "LanguageID"`,
  plus `VariantID` and `ProductKey`. The database comparison uses those names, and
  `ProductKey` is the marker that identifies a product index.
- **Document count without a private API.** A match-all query with `Take = 1` reports the
  real hit count in `IQueryResult.TotalCount`; `SearchInternal` raises `Take` to 10 when it
  is below 1, so 1 is the cheapest honest probe.
- **Every `DataViewModelBase` subclass must be named `*Model`.** The base constructor throws
  `Implementations inheriting Dynamicweb.CoreUI.Data.DataViewModelBase has to be called
  '{NameOfModel}Model'`. A nested section type called `ReportSection` blew up the whole
  screen at request time; it is `ReportSectionModel`.
- **The query provider returns null when the instance is down.**
  `LuceneIndexProvider.SearchInternal` starts with `if (!IsAvailable) return null;`, which
  the document browser surfaces as "build the index before browsing its documents".

## Code layout

- `Core/Search/SearchSpecs.cs` — the DW-free records (`RepositorySpec`, `IndexSpec`,
  `IndexFieldSpec`, `IndexInstanceSpec`, `QuerySpec`, `QueryGroupSpec`/`QueryClauseSpec`,
  `FacetGroupSpec`) and `ISearchSource`.
- `Core/Search/SearchCatalog.cs` — resolves queries and facet groups onto their index.
- `Core/Search/LuceneSemantics.cs` — the drop/collapse semantics above, in one place.
- `Core/Search/Rules/*.cs` — `IQueryLintRule` implementations IDX-W1..IDX-W17.
- `Core/Search/FieldUsageMap.cs` — the where-used report.
- `Core/Search/Dw/DwSearchSource.cs` — reads the repositories through DW's public API.
- `Core/Search/Dw/DwIndexDocuments.cs` — the live document read and the product comparison.
- `tests/…/QueryLintRuleTests.cs`, `FieldUsageMapTests.cs` — the rules, over hand-built specs.
