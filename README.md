# Truvio PowerTools

A growing suite of admin power tools for Dynamicweb 10 by
[JustDynamics](https://www.justdynamics.nl) — one backend app, one install, its own
**PowerTools** area in the administration, published as a NuGet package (visible in the
DW10 admin **Available apps** list).

Compatible with **Dynamicweb 10.8.4 and newer** through three package lines from one source.
The Base line targets net8.0 / DW 10.8.4, Discount targets net8.0 / DW 10.25.5, and Groups
targets net10.0 / DW 10.30.6. Price Explainer capabilities differ by line; see
[package lines and verification](docs/price-explainer.md). Older hosts retain solid badges
and omit screen subtitles where those APIs are unavailable.

Found a problem or have an idea? [Report it on GitHub](https://github.com/justdynamics/Truvio.Commerce.PowerTools/issues).

## Tools

| Tool | Section | What it does |
|---|---|---|
| **Content Access Viewer** (formerly Security Viewer) | Security | Pick a security account — a frontend role, a user group, or a user — and see exactly what content that account can access, across pages, grid rows, and paragraphs. Built for business users who use permissions for personalisation and need to verify who sees what. |
| **Experience Analyzer** | Security | What stands out about one account's content experience, and where two accounts differ: the pages only this role sees, the pages only that role sees, the gate behind each, and per-website tallies. With no comparison account the public (anonymous) experience is the baseline — including the pages a signed-in account has *lost*. Access analysis only, no page rendering. See [docs/experience-analyzer.md](docs/experience-analyzer.md). |
| **Content Access Warnings** | Security | Install-wide misconfiguration findings: ineffective group grants, gated sign-in pages, ignored legacy permission columns, orphaned grants. |
| **Operations Console** | Operations | Health of the install in one place: scheduled tasks (state, last/next run, who ran them, failures, stale tasks), integration activities (providers, last result, broken task→activity links), logs & storage (log folders by size, largest DB tables, retention settings, growth findings), recent changes (command log, audit, config timestamps). Rules OPS-W1…W9 — see [docs/operations-console.md](docs/operations-console.md). |
| **Index & Query Inspector** | Search | Repositories and indexes with build status and full schema, field where-used (dangling / unused fields), a 17-rule query linter (IDX-W1…W17, incl. the blank-parameter leak that returns the whole index), and a live document browser that diffs product documents against the database. See [docs/index-inspector.md](docs/index-inspector.md). |
| **Query Tester** | Search | Pick a query, supply parameter values, run it live: hits vs. defaults-only vs. index total, the executed Lucene query text, a clause-by-clause trace (active / dropped by blank parameter / unknown field / always true), per-clause impact, "why is document X not in the result", and ordered fix suggestions. See [docs/query-tester.md](docs/query-tester.md). |
| **PIM Quality** | PIM | Catalog-wide product data quality, built on DW's own completeness scores: which product families are incomplete (ranked, scoped by group and language), which single *field* is missing on the most products, which language layer is behind, missing variant combinations, duplicate assets, broken image paths, and completion rules assigned to nothing. Rules PIM-W1…W8 — see [docs/pim-quality.md](docs/pim-quality.md). |
| **Price Explainer** | Commerce | Found price, price-line discount, campaign savings and final unit price at the selected quantity; unit-price steps with their source; applicable price rows and rejected counts with bounded detail. Explains verified standard selection rules and shows provider prices without claiming a custom winner. Switch account, product, currency, shop, quantity and date in the toolbar. See [capabilities and limits](docs/price-explainer.md). |

**Settings** (PowerTools ▸ Settings) tune every tool: linter suppressions (rule ids, parameter names, queries), stale/bloat thresholds, picker caps, Price Explainer presets, per-section toggles. Suppressed findings are always shown as a "hidden by settings" count. See [docs/settings.md](docs/settings.md).

More tools are planned; each lands in its own section of the PowerTools area.

## Repository layout

The source is organised per feature, one type per file (enforced at build time by
`.editorconfig` + `Directory.Build.props`: StyleCop SA1402/SA1649 and IDE0130 are errors).

```
src/Truvio.Commerce.PowerTools/
  Features/<Feature>/        one folder per tool, e.g. PriceExplainer, IndexInspector
    Core/                    DW-free logic: specs, evaluators, rules (unit-tested)
    Dw/                      the Dynamicweb-backed readers behind the Core interfaces
    Screens/  Queries/  Models/  Commands/  Selectors/  Navigation/
                             the admin UI of the feature (CoreUI screens, data queries,
                             view models, commands, pickers, breadcrumb path providers)
  Shared/                    used by more than one feature: Diagnostics (Finding),
                             Principals (accounts), Currency, Navigation (area + sections),
                             Security (permission entity), AdminUI (badges, toolbar, pickers)
tests/Truvio.Commerce.PowerTools.Tests/
  Features/<Feature>/        mirrors the feature folders
```

Namespaces follow the folders: `Truvio.Commerce.PowerTools.Features.<Feature>.<Layer>`.

## Screens

All screens live in the dedicated **PowerTools** area of the admin navigation (between
Apps and Settings), grouped into per-tool-family sections. The area's sections are
visible only to users with access to the respective tools.

| Screen | What it shows |
|---|---|
| Content Access Viewer | Opens directly on the overview (anonymous frontend role, all websites); switch account (searchable slide-over picker, covering roles, groups and users) and website from the toolbar. |
| Content access | Every page with the account's effective level, its origin (set here / inherited / role default), and gating warnings. |
| Page audience | Drilldown for one page: the page verdict, then each grid row and paragraph with visible/hidden and the winning grant or deny. |
| Experience Analyzer | Opens on the anonymous baseline; pick the account, add a second one with "Compare with…", scope to a website. Only-A / only-B page lists with the gate explanation and an audit link into the page drilldown. |
| Warnings | Install-wide misconfiguration findings (see below). |
| Price Explainer | Pick the account (anonymous visitor or a user — searchable), then the product (searchable; variants listed separately). |
| Explanation | Price and line total, price steps, context and visibility, applicable matrix rows with Wins / Outranked / Shadowed / Discount line / Rejected verdicts, and capped discount evidence. Custom-provider or unverified-rule rows say Applies. Rejected counts open to bounded details. DW's raw quantity list remains separate. |

## Warning rules

| Rule | Severity | Finds |
|---|---|---|
| SECOPS-W1 | Critical / Warning | Group grants that don't gate: highest-wins resolution lets the broad frontend roles override a bare group grant unless the entity also carries an explicit deny for them. |
| SECOPS-W2 | Critical | A sign-in page (UserAuthentication app) that is itself denied to Anonymous or inactive — the anonymous-deny redirect flow dead-ends. |
| SECOPS-W3 | Warning | Populated legacy permission columns the DW10 runtime ignores — a false sense of gating. |
| SECOPS-W4 | Info | Permission rows referencing deleted groups. |

## How resolution is modelled

The viewer mirrors DW10's render-time rules: permission rows live in the permission entity
store keyed per page/grid row/paragraph; each identity of an account (frontend roles +
groups) contributes its explicit row or its role default (frontend roles default to Read);
the highest contribution wins; pages without rows inherit from the nearest ancestor
carrying rows; grid rows and paragraphs without rows follow their page. Administrator-type
accounts bypass checks entirely and are badged as such. The viewer is strictly read-only.

## How prices are explained

The found price comes from `PriceManager.FindPrice` with an explicit quantity and the selected
account, currency, country, shop and date. `Services.Prices.FindPrices` supplies the matching
rows in DW's order. The independent evaluator explains rejections and selects a winner only
for a verified host rule that the installed package can represent. Priorities and price-line
percentages are part of selection on newer hosts; lowest gross price alone is not sufficient.
Custom price providers disable winner claims.

The Discount and Groups lines show the returned price-line percentage separately from campaign
savings. Standard percentage campaigns use the selected quantity's found price, the configured
rounding and limits. Fixed amounts use DW's own calculation; extenders and amount-from-field
amounts retain DW's quantity-1 value and are flagged. Savings are combined using the configured
accumulate/highest/lowest setting. Final unit price subtracts both savings from the found price;
the line total multiplies that unit result by the chosen quantity. Both VAT views are shown.
This is a product-context explanation: cart-only conditions and storefront template logic are
outside it. The New Discount Experience explicitly suppresses campaign/final calculations.

Each applicable break plus quantity 1 is priced separately through the provider chain, with a
source column. DW's raw quantity list is also shown because it can contain duplicate breaks
and does not perform the same net-price selection. Rejected rows and out-of-scope discounts
are counted by reason with capped, expandable samples. See [the exact rules and limits](docs/price-explainer.md).

Visibility still mirrors `AssortmentService`: the account must hold an assortment containing
the product through a user/group permission or the anonymous flag. Products in no assortment
remain directly accessible but may be absent from assortment-filtered lists; the report warns.

## Install

- **App store**: DW10 admin → Apps → Available apps → search "PowerTools" → install.
- **Package reference**: add `Truvio.Commerce.PowerTools` to your `Dynamicweb.Host.Suite` project.
- **Manual**: build, copy `Truvio.Commerce.PowerTools.dll` into the host's `bin\`, restart.

Access to the screens can be managed like any other permission: the app registers a
"Truvio PowerTools" permission entity with one key per tool
(`truvio-powertools-security-viewer`, `truvio-powertools-price-explainer`, `truvio-powertools-operations`, `truvio-powertools-search`; `truvio-powertools-settings` needs Edit to save settings). Per DW semantics
each is open until an admin explicitly manages it; checks fail closed.

## Development

```
dotnet build
dotnet test
```

Pack happens on build (`GeneratePackageOnBuild`). The default local build uses the Base floor.
Use the release helper to rebuild, test, pack and verify each line:

```powershell
./.github/scripts/build-line.ps1 -Line Base
./.github/scripts/build-line.ps1 -Line Discount
./.github/scripts/build-line.ps1 -Line Groups
```

Install the .NET 8 and .NET 10 SDKs/runtimes for all three lines. The helper derives
`0.13.0-beta`, `0.13.1-beta` and `0.13.2-beta` from the Base release version. The Base
patch is a multiple of ten (`10n`); Discount adds 1 and Groups adds 2. The next patch
release uses `0.13.10`, `0.13.11` and `0.13.12`, preserving any prerelease suffix.
This ordering also works for stable releases. Four-part package versions cannot be used:
DW can interpret the extra number as part of the add-in name and silently skip the DLL.

PR and branch checks build all three without publishing. The tag remains `v<Base version>`
(for this release, `v0.13.0-beta`); it runs all checks before NuGet Trusted Publishing and
attaches every line to the GitHub release. The guards verify the packed manifest resolves
to the intended add-in identity and version, plus the DLL's actual references, target
framework and NuGet dependency minima. CI tests the version mapping and rejects a packed
four-part-version regression case. A previous build against a newer DW version cannot
silently pass as a lower-floor package.
