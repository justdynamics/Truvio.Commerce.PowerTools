# PIM Quality

**PowerTools ▸ PIM** — read-only. One place that answers *how complete is this catalog, which single
field should we fill first, and which completion rules govern nothing?*

Permission: `truvio-powertools-pim` (Read). The section and every node are hidden without it.

Community requests this merges (from the Dynamicweb feature-request tracker): an incomplete-products
query (#530), missing variant combinations (#434), language-layer gaps (#565) and duplicate asset rows
(#460).

## What DW already does — and where this starts

Completeness is a real, supported DW feature: `CompletionRuleService` computes the score, the product
list can show a Completeness column, and one product at a time can be inspected per rule and per
language. **PowerTools never recomputes that score** — it reports DW's own numbers.

What DW has no screen for is the catalog as a whole: ranking products worst-first across a group,
naming the *field* whose absence costs the most, comparing language layers, or finding completion rules
assigned to nothing. That aggregation is this tool.

Because the scores come from `CompletionRuleService` directly and not from DW's v2 completeness UI,
these screens work on installs where DW's own `CompletenessFeature` flag is switched **off**.

## Screens

| Node | Screen | What it answers |
|---|---|---|
| Catalog quality | `PimQualityScreen` | Everything at once: verdict, average completeness, the "fix these fields first" ranking, all findings |
| Completeness explorer | `PimCompletenessScreen` → `PimProductQualityScreen` | Which product families are incomplete, worst first; per product: which field is missing, under which rule, in which language, on which variant |
| Rules & workflows | `PimGovernanceScreen` | Which completion rules are assigned to anything, and which workflows the catalog references |

Report screens use `OverviewScreenBase` + `HtmlBlock` tables rather than the list grid: the grid gives
every column equal width and clips overflow, and explanations are long text. List screens keep to six
short columns for the same reason.

**Family rows, not variant rows.** The explorer lists one row per master product with the family score
— DW itself scores a family, and a 12 000-part catalog with variants would otherwise render tens of
thousands of rows. Variants unfold on the drill-down screen, which lists each one with its own score.

**Scope** is a group picker (searchable slide-over — a catalog can hold thousands of groups) and a
language switch, both in the top bar next to Actions. Every scope is a shareable URL.

## Rules

Pure, in `Core/Pim/Rules/`, unit-tested against hand-built snapshots. Ids are stable.

The thresholds below are the shipped defaults; PIM-W1's 60 %, PIM-W2's 25 % share and the product scan
cap are all configurable under **PowerTools ▸ Settings**.

| Id | Rule | Fires when | Severity |
|---|---|---|---|
| PIM-W1 | `IncompleteProductRule` | Product scores below **60 %** completeness | Warning (Critical at 0 %) |
| PIM-W2 | `CommonFieldGapRule` | One field is missing on **≥ 25 %** of the products scanned — the "fix this first" signal | Info |
| PIM-W3 | `LanguageGapRule` | A language layer averages more than *(100 − threshold)* points behind the default language | Warning |
| PIM-W4 | `VariantGapRule` | A product's variant groups allow combinations that do not exist | Warning |
| PIM-W5 | `DuplicateAssetRule` | The same asset path is attached to one product more than once | Info |
| PIM-W6 | `DeadCompletionRuleRule` | A completion rule is assigned to no shop, group or query — it scores nothing | Warning |
| PIM-W7 | `UnusedCategoryRule` | A product category is used by no group | Info |
| PIM-W8 | `BrokenImageRule` | A product resolves to an image path with no file behind it | Warning |
| PIM-E1 | `PimQualityEngine` | A rule threw — one broken reader must not hide the other findings | Info |

Why PIM-W3's tolerance is derived rather than configured separately: an install that accepts 60 %
completeness has already said it accepts a 40-point spread between its language layers. A separate
knob would be a second way to say the same thing. The floor is 5 points, so a strict install still
gets a usable rule.

Why PIM-W4 reports a number instead of a list: three variant groups of ten options are a thousand
combinations, and real catalogs have products whose potential count runs into the millions. Above
1 000 potential combinations the rule states the count and stops — a gap that size is almost always
deliberate, and "fill in these 4 000 000 combinations" is not advice. Below it, up to five missing
combination ids are named.

Why PIM-W2 measures against the *scanned* set: the scan is capped (default 200 products), so the
percentage is of what was actually scored. When the scope holds more products than the cap, the
finding says so rather than implying it measured the whole catalog.

## Suppression is never silent

`PimSuppressedRules` mutes rule ids (a trailing `*` matches a prefix, e.g. `PIM-W7*`). The catalog
quality screen always appends a row saying how many findings were hidden — a muted rule is never
invisible.

## DW facts verified (decompiled 10.8.4, the floor this package targets)

Every API below exists unchanged at 10.8.4; nothing here needs a newer host.

- **Scores** — `Services.CompletionRules.CalculateProductCompletenessForMultipleFamilies(IEnumerable<string>, CompletenessOptions)`
  → `IDictionary<string, CompletnessResult>` (note DW's spelling of `Completness`). Per-language:
  `CalculateProductCompletenessForLanguages(productId, options, languageIds)`. `CompletnessResult.Value`
  is documented as [0, 100].
- **Which field is missing** — `CompletnessResult.HasFieldValue(productId, variantId, languageId, ProductField)`,
  but **`ProductValueExcludedFromCalculations(...)` must be checked first**: without it, inherited and
  out-of-scope fields are reported as missing and every product looks broken.
- **Resolving a rule's fields** — `CompletionRule.Fields` is obsolete in favour of `FieldSystemNames`,
  and DW's own resolver for them (`ProductField.GetProductFieldsByUniqueNames`) is `internal`. The
  source instead looks the names up in `ProductField.GetAllEditableProductFields()`, which is public
  and keyed by system name — the same lookup DW performs, through the public surface.
- **Products** — `Services.Products.GetProductsBySearch(ProductSearchFilter)` with `GroupIds`,
  `LanguageIds`, `PageSize` and `VariantFilter`. The enum member for master-only rows is
  `VariantStateFilter.Masters` (`All`, `Masters`, `Variants`).
- **Variant gaps** — `Services.Variants.PotentialVariantCount(productId)` → `ulong?` guards the whole
  rule; `Services.VariantCombinations.GetVariantCombinations(productId)` is what exists, and
  `GetAllPossibleVariantIds(variantGroups, languageId)` (with
  `Services.VariantGroups.GetVariantGroupsByProductId`) is only called for small combination spaces.
- **Duplicate assets** — `Services.Details.GetDetailsBulk(IEnumerable<ProductKey>, detailType, excludeDefaultImage)`
  → `Dictionary<string, List<Detail>>`, grouped by `Detail.Value`. One call for the whole page.
- **Broken images** — `Services.ProductImages` (**not** `Services.Products.ProductImages`).
  `GetImagePath(product)` resolves patterns and the group/shop default. The existence check is
  `FileExists(path)` — **`CheckPhysicalPath` only sanitises invalid characters and does not test the
  filesystem**, despite the name.
- **Dead rules** — `Services.CompletionRules.GetUsages(IEnumerable<CompletionRule>)` (bulk overload);
  an empty usage list means dead. `CompletionSettingsSource.Type` is `Shop | Query | Group`, with
  `Name` / `ParentName` for the message.
- **Unused categories** — `Services.ProductCategories.GetCategoriesUsages()` → `Dictionary<string,int>`.
- **Workflows** — `Dynamicweb.Ecommerce.Workflows.WorkflowServiceExtensions.GetWorkflowsInUseByGroups()`
  / `.GetWorkflowsInUseByProducts()`, extension methods on a `Dynamicweb.Security.Workflows.WorkflowService`
  instance. DW marks manual construction obsolete in favour of its container, but the resolver is not
  public at this floor; the call is wrapped so a future removal degrades the section to empty.
- `CompletenessFeature` — DW's v2 completeness flag — is **deliberately not referenced**: it does not
  exist at 10.8.4, and the scores are computed identically whether it is on or off.

## Known gap: workflow stuck-state ageing

"Products stuck in a workflow state for N days" is an attractive signal and is **not** in this tool.
The workflow extensions above answer only *which workflows are referenced*; DW exposes no read API for
how long a product has sat in a state. Adding it would mean a direct database read, which no PowerTools
tool does today. The Rules & workflows screen therefore reports references, not ageing.

## Performance

The bulk completeness call is the expensive part of every screen here. Mitigations, all visible to the
user rather than silent:

- The product scan is capped (`PimProductCap`, default 200) and the explorer renders the standard
  trailing "*N* more products not shown — use the search to narrow the list" row.
- The catalog overview says when it scanned a sample rather than everything, and points at both the
  setting and the group picker.
- The catalog-wide passes (variants, assets, images) share one product enumeration rather than
  querying per rule.

**Not yet measured on a large catalog.** The obvious test host is the marine demo (~12 000 parts);
timing `CalculateProductCompletenessForMultipleFamilies` there decides whether the overview should
keep scanning or start sampling.

## Verified on

Builds green at both `-p:DynamicwebVersion=10.8.4` (the advertised floor) and `10.27.9`; no new package
reference. 48 unit tests over hand-built snapshots, no DW host required. **Not yet run against a live
catalog** — the rules are exercised against fakes, and the DW reads above are verified by decompile,
not by observation.


## Verified live (cabp, DW 10.27.9, 2026-08-24)

All four screens rendered against real data (58 products, 1 completion rule, 2 variant
groups, 17 languages): the catalog overview produced genuine PIM-W1 findings (products at 33%
against the 60% threshold) and PIM-W2 common-gap rankings ("missing on 38 of 39 scanned
products"), the explorer listed 39 family rows worst-first with per-row drill-down, the
product screen named the real rule behind each missing field, and the governance screen showed
the rule with its assignments. Open item: `CalculateProductCompletenessForMultipleFamilies`
has not yet been timed on a large catalog (~12k products) — the marine demo receives the
released package and is the perf test host; the product cap + trailing "N more" row bound the
cost until then.


## Preview in shop (0.9.1)

The product screens carry a "Preview in shop" action that opens the storefront PDP in a new
tab — on the PowerTools Product quality screen, on the Price Explainer toolbar, and injected
server-side into DW's **own** product edit screen (a `ScreenInjector`, so the AdminUI
JS-injection limitation does not apply; any resolution failure simply hides the button). The
URL is DW's always-valid entry `/Default.aspx?ID={page}&ProductID={id}` — the frontend
rewrites it to the friendly URL. The target page comes from the **Preview pages** setting
(`SHOPID=PageId` per line, bare id = default) or, unconfigured, from auto-detection of a
Swift product-details page on the shop's website. The tab renders as the browser's own
frontend session: a login-gated storefront asks for sign-in first, and there is no
"view as user" (DW has no supported mechanism for that).
