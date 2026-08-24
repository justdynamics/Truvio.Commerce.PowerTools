# PIM quality tools — research & build plan

Research note, not user documentation. Written 2026-08-24 against **Dynamicweb.Ecommerce
10.25.4-prerelease** (feature check) and **10.8.4** (the floor the csproj advertises:
"Requires Dynamicweb 10.8 or newer"). Every API cited below was read out of the decompiled
assembly; where an API exists only above the floor it is called out explicitly.

Source of the idea: backlog item **#6 "PIM Data Quality Auditor"** in the
`powerapps-idea-backlog` memory — mined from the 648 issues on the DW feature-request
tracker.

---

## (a) Backlog inventory — every PIM-quality idea

From `powerapps-idea-backlog.md`, item 6 verbatim:

> **PIM Data Quality Auditor** — missing variant combos #434, incomplete-products query
> #530, language-layer gaps #565, duplicate asset rows #460; viewer→paid bulk-fix upsell.

Broken out, plus the adjacent items that touch product data:

| # | Signal | Backlog source | Notes |
| --- | --- | --- | --- |
| Q1 | Incomplete products, catalog-wide | #530 "incomplete-products query" | The headline ask. DW scores one product at a time; nobody can rank the catalog. |
| Q2 | Missing variant combinations | #434 | Product has variant groups but not every option combination exists. |
| Q3 | Language-layer gaps | #565 | Field filled in the default language, empty in another `EcomLanguage`. |
| Q4 | Duplicate asset rows | #460 | Same asset attached twice to a product (`EcomDetails`). |
| Q5 | Orphan / unused objects | item 4 "Dependency & Orphan Explorer" | Product in no group; category used by no group; completion rule assigned nowhere. |
| Q6 | Who changed what | item 7 "Change Journal" | **Explicit coverage gap** — #592: no notification subscribers for Product/CategoryField, so a change journal for PIM has to poll. Out of scope here. |

Adjacent, already shipped — reuse, don't rebuild:
- **Field where-used** (Index & Query Inspector) already answers "which query/sort/facet
  references this *index* field". PIM field usage is a different axis (category fields on
  products), so there is no overlap, but the screen is the design precedent.
- **Price Explainer** already owns per-product commerce truth and has the product picker
  (`ProductPickQuery`) and `ProductPickCap` setting this plan reuses.

Constraint carried over from the backlog: **#437 — AddIn-injected JS/CSS into the AdminUI
chrome does not survive a refresh.** No persistent banners or badges injected into DW's own
product screens; PowerTools ships parallel screens only. This is why the plan below never
proposes "add a column to DW's product list".

---

## (b) What DW already ships — do not duplicate

Read from `Dynamicweb.Products.UI` 10.27.9 and `Dynamicweb.Ecommerce`.

**Completeness is a real, supported DW feature.** The engine is
`Dynamicweb.Ecommerce.Products.CompletionRules.CompletionRuleService`, reachable as
`Dynamicweb.Ecommerce.Services.CompletionRules` (`Services.cs`). DW's own UI exposes it as:

| DW surface | Type | Scope |
| --- | --- | --- |
| Completeness column on the product list | `ProductDataModel.Completeness` (`[ConfigurableProperty("Completeness", …)]`) | one product per row, opt-in column |
| Per-rule completeness for a product | `ProductCompletenessRulesListScreen` — "List screen showing per-rule completeness values for **a product** (completeness v2)" | one product |
| Per-field, per-language state | `ProductFieldCompletionDataModel.CompletenessPerLanguage` / `.CompletenessValues` | one product |
| Rule CRUD + assignment | `CompletionRuleSaveCommand`, `CompletionRuleDeleteCommand`, `CompletionRuleRemoveFromGroupCommand`, `CompletionRuleRemoveFromShopCommand` | settings |
| PIM dashboard widgets | `ProductCountWidget`, `ActiveProductsCountWidget`, `PublishedProductsCountWidget`, `UnpublishedProductsCountWidget`, `LastChangedProductsWidget`, `NewlyArrivedProductsWidget`, `ProductQueryListWidget` | counts and recency only |

**Feature flag.** The v2 completeness UI is gated:
`ProductDataModel` calls `obj.IsActive<CompletenessFeature>()` before filling
`CompletenessFields`. `CompletenessFeature` (`FeatureBase`, default **false**, "Enables a
new way of calculating product completeness with a field for each rule") exists at 10.25
but **does not exist at 10.8.4** — so PowerTools must not compile against it
unconditionally. See "Version gating" below.

**The gap PowerTools fills.** Nothing in DW aggregates across the catalog. There is no
screen that answers:

- which 50 products are worst, ranked, across a group/shop;
- which *field* is the most common cause of incompleteness (fix one field, lift 400 products);
- which language layer is behind;
- which products have variant groups but missing combinations;
- which completion rules are assigned to nothing (dead config);
- which products resolve to an image path with no file behind it.

No widget, screen or API in `Dynamicweb.Products.UI` does any of these. That is the product.

---

## (c) Proposed PowerTools "PIM" section

New `NavigationSection<PowerToolsArea>` between Commerce (30) and Operations. Follows
`SearchSection` exactly: `Name`, `Sort`, `ShouldShow()` gated on a function grant plus a
section toggle.

```csharp
// AdminUI/Tree/PimSection.cs
public sealed class PimSection : NavigationSection<PowerToolsArea>
{
    public PimSection(NavigationContext context) : base(context)
    {
        Name = "PIM";
        Sort = 35;                       // Commerce is 30, Operations 50
    }

    public override bool ShouldShow() =>
        PowerToolsAccess.CanUsePim() && DwPowerToolsSettings.Current.PimSectionEnabled;
}
```

Requires, following the established pattern:
- `PowerToolsPermissionEntity.PimKey = "truvio-powertools-pim"`, added to `AllKeys` **and**
  `ToolKeys`; `PowerToolsAccess.CanUsePim()`.
- `PowerToolsSettingKeys`: `PimSectionEnabled`, `PimProductCap`, `PimCompletenessThreshold`,
  `PimSuppressedRules` (mirrors `SuppressedWarningRules`).
- `PimNavigationPaths` + `PimNodeProvider` (clone `SearchNavigationPaths` / `SearchNodeProvider`).

### The shared data layer (build once, all screens use it)

```
Core/Pim/
  IPimQualitySource.cs      — interface, so rules are testable against a fake
  PimSnapshot.cs            — records: ProductQuality, RuleUsage, VariantGap, AssetRow, LanguageGap
  PimQualityEngine.cs       — runs IPimRule[] over the snapshot -> Finding[]  (clone OperationsHealthEngine)
  PimScope.cs               — group / shop / language / cap; what the toolbar pickers set
  Rules/
    CompletenessRules.cs
    VariantGapRule.cs
    RuleHygieneRule.cs
    AssetRules.cs
  Dw/
    DwPimSource.cs          — the only file that touches Dynamicweb.*
```

This mirrors `Core/Operations/` one-for-one (`IOperationsSource` + `OperationsSnapshot` +
`OperationsHealthEngine` + `Rules/` + `Dw/DwOperationsSource.cs`), so the existing
`Finding` / `FindingSeverity` / severity-ordering / settings-suppression / "N findings
hidden by settings" machinery is reused unchanged.

### Screen 1 — Completeness Explorer  `PimCompletenessScreen` (list)

**Answers:** which products in this scope are incomplete, how badly, and what is missing.

One row per product: number, name, language, completeness badge (0–100), worst rule,
missing-field count, first 3 missing field names. Toolbar: group picker + language picker
(the `ToolbarSwitch.AddPicker` / `SelectorScreen` pattern from the Price Explainer), plus
the built-in search box (`[ConfigurableProperty(..., isSearchable: true)]` +
`DataQueryListBase`). Row click → Screen 2.

| Data | Verified API |
| --- | --- |
| Enumerate products, paged, scoped | `Services.Products.GetProductsBySearch(ProductSearchFilter)` → `ProductSearchResult { TotalCount, IList<Product> Products }`. Filter carries `GroupIds`, `LanguageIds`, `PageNumber`, `PageSize`, `ActiveFilter`, `VariantFilter`, `IncludeOrphanedProducts`, `ShopType`, `UpdatedFrom/To`, `CreatedFrom/To`. Already used by `ProductPickQuery`. |
| Bulk completeness scores | `Services.CompletionRules.CalculateProductCompletenessForMultipleFamilies(IEnumerable<string> productIds, CompletenessOptions)` → `IDictionary<string, CompletnessResult>` (10.8.4 line 583) |
| Score value | `CompletnessResult.Value` — int, documented range [0, 100] |
| Which field is missing | `CompletnessResult.HasFieldValue(productId, productVariantId, productLanguageId, ProductField)` → bool |
| Field genuinely in scope | `CompletnessResult.ProductValueExcludedFromCalculations(productId, productVariantId, productLanguageId, ProductField)` — **must be checked first**, otherwise inherited/out-of-scope fields are reported as missing |
| Rules in effect | `CompletionRule.Fields` (`IEnumerable<ProductField>`), `.FieldSystemNames`, `.Name`, `.ExcludeVariants`; `Services.CompletionRules.GetGroupsRules(IEnumerable<Group>)`, `.GetShopsRules(IEnumerable<Shop>)`, `.GetAll()` |
| Options object | `CompletenessOptions { DefaultLanguageId, LanguagesIds, Rules, Groups }` |
| Languages | `Services.Languages.GetLanguages()`, `.GetDefaultLanguageId()`, `.GetDefaultLanguage()` |
| Groups | `Services.ProductGroups.GetGroups(string languageId)`, `.GetGroup(groupId, languageId)` |

**Effort: M.** No new UI mechanics — it is `ListScreenBase` + two toolbar pickers, both
already solved in this codebase.

**Perf — the one real risk.** `CalculateProductCompletenessForMultipleFamilies` is the bulk
entry point, but it has not been timed against a large catalog (the marine demo carries
~12k parts and is the obvious test host). Mitigate exactly like the other tools: page the
enumeration and cap it with `PimProductCap` (default 200, same shape as `ProductPickCap`),
and render the established "N more products not shown — use the search to narrow the list"
trailing row. **Time this before building Screen 3**, because the aggregate screen depends
on scanning more rows than one page.

### Screen 2 — Product Quality Explainer  `PimProductQualityScreen` (overview)

**Answers:** for this one product, exactly what is missing, per rule × per language × per
variant, and what filling it would be worth.

`OverviewScreenBase<PimProductQualityModel>`: infobar (score badge, rules applied, fields
missing, languages behind), then one `HtmlBlock` table per completion rule — a field ×
language matrix of filled / missing / not-applicable — and a variant section. This is the
Price Explainer's "why" DNA applied to PIM, and the reason the section is worth building at
all rather than shipping a bare list.

| Data | Verified API |
| --- | --- |
| Per-language scores for one product | `CalculateProductCompletenessForLanguages(string productId, CompletenessOptions options, IEnumerable<string> languageIds)` → `Dictionary<string, CompletnessResult>` |
| Per-group scores for one product | `CalculateProductCompletenesForGroups(string productId, IEnumerable<string> groupIds)` → `Dictionary<string, CompletnessResult>` (note DW's typo in the method name — `Completenes`) |
| Single score | `CalculateProductCompleteness(string productId, CompletenessOptions)` / `(IEnumerable<Product> productFamily, CompletenessOptions)` |
| Product family (master + variants) | `Services.Products.GetProductsAndVariantsByProduct(Product)` |
| Field-level filled/missing | `CompletnessResult.HasFieldValue(...)` + `ProductValueExcludedFromCalculations(...)` |
| Category fields on the product | `Services.ProductCategories.GetCategories(Product product, bool includeProductProperties)` |

**Effort: M.** Straight `OverviewScreenBase` + inline-styled HTML tables — the pattern
`OperationsHealthScreen` and the Price Explainer already use (list grids clip long text;
overview + HtmlBlock does not).

### Screen 3 — Catalog Quality Overview  `PimQualityScreen` (overview, the landing screen)

**Answers:** how healthy is the catalog, and which single fix moves the needle most.

Infobar: products audited, average completeness, count below threshold, variant gaps,
broken images, dead rules. Then the findings table (reusing `OpsHtml.Table`-style rendering
over `Finding[]`), then a **"worst fields" ranking** — the field that is missing on the most
products. That ranking is the differentiator: it converts a score into a work order.

Rules (`IPimRule`, ids in a `PIM-Wn` series to match `SECOPS-Wn` / `OPS-Wn` / `IDX-Wn`):

| Rule | Severity | Catches | Backlog |
| --- | --- | --- | --- |
| PIM-W1 | Warning | Product below the completeness threshold (configurable, default 60) | #530 |
| PIM-W2 | Info | Field missing on more than N% of scanned products — the "fix this first" signal | #530 |
| PIM-W3 | Warning | Language layer materially behind the default (score delta > threshold) | #565 |
| PIM-W4 | Warning | Product has variant groups but is missing possible combinations | #434 |
| PIM-W5 | Info | Duplicate asset rows on a product | #460 |
| PIM-W6 | Warning | Completion rule assigned to no shop, group or query — dead config | #6/Q5 |
| PIM-W7 | Info | Category used by no group | Q5 |
| PIM-W8 | Warning | Product resolves to an image path with no file behind it | Q5 |

| Rule data | Verified API |
| --- | --- |
| Variant gap (W4) | `Services.VariantCombinations.GetAllPossibleVariantIds(IEnumerable<VariantGroup> variantGroups, string languageId)` → `IList<string>` **minus** `.GetVariantCombinations(string productId)` → `IList<VariantCombination>`. Guard with `Services.Variants.PotentialVariantCount(string productId)` → `ulong?` — a combinatorial explosion must be reported as a number, never enumerated. |
| Variant groups on a product | `Services.Variants.GetOptions(string productId, VariantGroup)`, `Services.VariantGroups`, `Services.Products.GetProductsByVariantGroup(VariantGroup)` |
| Duplicate assets (W5) | `Services.Details.GetDetailsBulk(IEnumerable<ProductKey> productKeys, string? detailType, bool excludeDefaultImage)` → `Dictionary<string, List<Detail>>` — group by path, count > 1. Bulk, one call. |
| Dead rules (W6) | `Services.CompletionRules.GetUsages(CompletionRule)` → `IEnumerable<CompletionSettingsSource>`; empty = dead. `CompletionSettingsSource.Type` is `Shop | Query | Group`, plus `.Name`, `.ParentName`, `.ParentType` for the message. Bulk overload: `GetUsages(IEnumerable<CompletionRule>)`. |
| Unused categories (W7) | `Services.ProductCategories.GetCategoriesUsages()` → `Dictionary<string,int>`; also `.GetUsageCount(Category)`, `.GetUsageGroupsAsCollection(Category)` |
| Broken images (W8) | `Services.Products.ProductImages` → `ProductImageService.GetImagePaths(IEnumerable<Product>, string groupId, bool ignoreNoPictureSetting)` → `Dictionary<ProductKey,string>` (bulk), then `.CheckPhysicalPath(string image, bool saveWildCards = false)` / `.FindFilesByImagePattern(imageCompiled, imageFolder, searchSubfolders, onlyExisting: true)` |
| Orphan products (Q5) | `Product.Groups` (`GroupCollection`) empty, or `Product.PrimaryGroupId` null. **Unverified perf** — `Groups` may lazy-load per product (N+1). Prefer deriving from `ProductSearchFilter.IncludeOrphanedProducts` (count with it true vs false) and confirm on a real catalog before shipping this as a rule. |

**Effort: L** (engine + 8 rules + aggregation), or **M** if it ships with W1/W2/W6 only and
the rest land incrementally — which is the recommended slicing.

### Screen 4 — Workflow & rule hygiene  `PimGovernanceScreen` (list) — optional, later

Completion rules with their assignments and orphan state; product workflows in use.

| Data | Verified API |
| --- | --- |
| Rules + usages | `GetAll()` + `GetUsages(IEnumerable<CompletionRule>)` |
| Workflows referenced by groups / products | `Dynamicweb.Ecommerce.Workflows.WorkflowServiceExtensions.GetWorkflowsInUseByGroups(this WorkflowService)` and `.GetWorkflowsInUseByProducts(this WorkflowService)` (extension methods on `Dynamicweb.Security.Workflows.WorkflowService`) |

**Effort: S.** Mostly a table over data W6 already gathers. **Note:** "products stuck in a
workflow state for N days" — an attractive signal — has **no verified read API**; the
extensions above only answer *which workflows are referenced*. Do not promise stuck-state
ageing until an API is found (or accept a direct DB read, which no PowerTools tool does today).

### Version gating

`CompletenessFeature` does not exist at the 10.8.4 floor. Everything else cited here does —
the entire `CompletionRuleService` public surface (all Calculate* overloads, `GetUsages`,
`GetGroupsRules`, `GetShopsRules`), `VariantCombinationService`, `DetailService.GetDetailsBulk`,
`ProductImageService`, `ProductCategoryService.GetCategoriesUsages` are all present at
10.8.4 unchanged. So:

- Do **not** reference `CompletenessFeature` directly. If the flag's state matters, resolve
  it reflectively or add a csproj constant in the existing style
  (`DW_HAS_SCREEN_EXPLANATION`, `DW_HAS_OUTLINE_BADGES`, `DW_DROPS_UNKNOWN_CLAUSE_FIELD`),
  e.g. `DW_HAS_COMPLETENESS_FEATURE` for ≥ 10.9.
- The scores themselves are computed the same way with the flag off, so the tools work on
  every supported host. Worth stating in the user docs: PowerTools reports completeness
  even where DW's own v2 completeness UI is switched off.

### Tests

Follow `OperationsRuleTests` / `WarningRuleTests`: a `FakePimQualitySource` (cf.
`FakeContentSecuritySource`) plus spec builders (cf. `SearchSpecBuilders`,
`OperationsTestData`). Every rule is pure over `PimSnapshot`, so all eight are unit-testable
with no DW host. Threshold configurability gets a test in `ConfigurableThresholdTests`.

---

## (d) Effort summary

| Screen | Effort | Depends on |
| --- | --- | --- |
| Shared `Core/Pim` layer + `DwPimSource` | M | — |
| 1. Completeness Explorer | M | shared layer |
| 2. Product Quality Explainer | M | shared layer |
| 3. Catalog Quality Overview | L (M if sliced) | shared layer, perf answer from #1 |
| 4. Workflow & rule hygiene | S | rule-usage data from #3 |

Section scaffolding (permission key, settings keys, section, node provider, paths) is **S**
and is carried by whichever screen ships first.

---

## (e) Build order — recommendation

**Build Screen 1, the Completeness Explorer, first.**

Why it, and not the overview that would eventually be the landing screen:

1. **It builds the foundation either way.** It forces `DwPimSource`, `PimSnapshot` and the
   scope/paging model into existence — every other screen is then additive.
2. **It answers the one perf question that could invalidate the whole section**
   (bulk completeness over a large catalog) on the cheapest possible screen, before the
   aggregate screen is built on top of that assumption.
3. **It is the backlog's headline ask** (#530, "incomplete-products query") and is
   immediately useful on its own — a ranked, scoped, searchable list of what to fix.
4. **It has no unsolved UI mechanics.** `ListScreenBase` + toolbar pickers + cap + trailing
   "N more" row are all patterns already shipped in this codebase.

Then: **2** (drill-down; makes 1's rows clickable and delivers the "why"), **3** (aggregate
landing screen, sliced W1/W2/W6 first, remaining rules incrementally), **4** (optional).

Ship 1 + 2 together as the `0.8.0-beta` "PIM Quality" section if a release needs a coherent
story — a list that ranks and a page that explains is a complete tool; the overview then
upgrades it rather than completing it.

### Screen 1, concretely — the next session's checklist

1. `AdminUI/Security/PowerToolsPermissions.cs` — add `PimKey`, extend `AllKeys` + `ToolKeys`,
   add `CanUsePim()`.
2. `Core/Settings/PowerToolsSettingKeys.cs` — add `PimSectionEnabled` (default true),
   `PimProductCap` (default 200), `PimCompletenessThreshold` (default 60); surface them in
   `PowerToolsSettings` + `DwPowerToolsSettings` + the settings screen, as the other tools do.
3. `AdminUI/Tree/PimSection.cs`, `PimNodeProvider.cs`, `PimNavigationPaths.cs` — clone the
   Search trio; node "Completeness" → `NavigateScreenAction.To<PimCompletenessScreen>()`.
4. `Core/Pim/` — `IPimQualitySource`, `PimSnapshot` (`ProductQuality` record: product id,
   variant id, language, number, name, score, worst rule, missing field names), `PimScope`,
   `Dw/DwPimSource` implementing it with `GetProductsBySearch` +
   `CalculateProductCompletenessForMultipleFamilies` + `HasFieldValue`/
   `ProductValueExcludedFromCalculations`.
5. `AdminUI/Models/PimCompletenessModel.cs` — `[ConfigurableProperty(..., isSearchable: true)]`
   on number/name so the toolbar search box appears.
6. `AdminUI/Queries/PimCompletenessQuery.cs` — `DataQueryListBase<…>`; public properties
   only for real URL state (`GroupId`, `LanguageId`) — computed values via methods, never
   properties, or they leak into the URL.
7. `AdminUI/Screens/PimCompletenessScreen.cs` — `ListScreenBase`, `GetCell` returning a
   `Badge` for the score (text-only: a Badge with an Icon renders icon-only and drops the value).
8. Tests: `PimCompletenessTests` over a `FakePimQualitySource`.
9. Docs: `docs/pim-quality.md` in the voice of `docs/index-inspector.md`; add the tool to the
   csproj `<Description>` bullet list.

### Open questions to settle while building

- Timing of `CalculateProductCompletenessForMultipleFamilies` on ~12k products (marine demo).
  Drives whether Screen 3 scans or samples.
- Whether `Product.Groups` lazy-loads per product (decides if the orphan rule is cheap).
- Whether completeness should be reported per variant or per family by default —
  `CompletionRule.ExcludeVariants` exists per rule, so the snapshot must carry both.
