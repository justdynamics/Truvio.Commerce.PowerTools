# Truvio PowerTools

A growing suite of admin power tools for Dynamicweb 10 by
[JustDynamics](https://www.justdynamics.nl) — one backend app, one install, its own
**PowerTools** area in the administration, published as a NuGet package (visible in the
DW10 admin **Available apps** list).

Compatible with **Dynamicweb 10.8 and newer** (the published package is compiled against
the 10.8 API; hosts below 10.23/10.24 get solid instead of outlined badges and no screen
subtitles — everything else is identical).

Found a problem or have an idea? [Report it on GitHub](https://github.com/justdynamics/Truvio.Commerce.PowerTools/issues).

## Tools

| Tool | Section | What it does |
|---|---|---|
| **Content Access Viewer** (formerly Security Viewer) | Security | Pick a security account — a frontend role, a user group, or a user — and see exactly what content that account can access, across pages, grid rows, and paragraphs. Built for business users who use permissions for personalisation and need to verify who sees what. |
| **Content Access Warnings** | Security | Install-wide misconfiguration findings: ineffective group grants, gated sign-in pages, ignored legacy permission columns, orphaned grants. |
| **Operations Console** | Operations | Health of the install in one place: scheduled tasks (state, last/next run, who ran them, failures, stale tasks), integration activities (providers, last result, broken task→activity links), logs & storage (log folders by size, largest DB tables, retention settings, growth findings), recent changes (command log, audit, config timestamps). Rules OPS-W1…W9 — see [docs/operations-console.md](docs/operations-console.md). |
| **Index & Query Inspector** | Search | Repositories and indexes with build status and full schema, field where-used (dangling / unused fields), a 17-rule query linter (IDX-W1…W17, incl. the blank-parameter leak that returns the whole index), and a live document browser that diffs product documents against the database. See [docs/index-inspector.md](docs/index-inspector.md). |
| **Price Explainer** | Commerce | Pick a user (or the anonymous visitor) and a product, and see whether they can see it and what they pay — and *why*: which assortment grants or blocks it, which price-matrix row wins and why every other row lost, which product discounts apply. Switch currency, shop, quantity and date from the Actions menu. |

More tools are planned; each lands in its own section of the PowerTools area.

## Screens

All screens live in the dedicated **PowerTools** area of the admin navigation (between
Apps and Settings), grouped into per-tool-family sections. The area's sections are
visible only to users with access to the respective tools.

| Screen | What it shows |
|---|---|
| Content Access Viewer | Pick the account to inspect (roles, groups, users) — searchable (name, username, e-mail, group name) and paged. |
| Content access | Every page with the account's effective level, its origin (set here / inherited / role default), and gating warnings. |
| Page audience | Drilldown for one page: the page verdict, then each grid row and paragraph with visible/hidden and the winning grant or deny. |
| Warnings | Install-wide misconfiguration findings (see below). |
| Price Explainer | Pick the account (anonymous visitor or a user — searchable), then the product (searchable; variants listed separately). |
| Explanation | Result (sees it / price before discounts / discounts / pays), the evaluated context, every assortment with held/grants/not-held, every price-matrix row with wins/shadowed/rejected and the exact reason, every active product discount with applied/rejected/not-applied and the reason. |

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

The Price Explainer runs two things side by side. DW's own engine (`PriceManager.GetPrice`,
`DiscountInfoCollection`) produces the authoritative numbers for the chosen user, currency,
country, shop, quantity and date. Independently, the explainer mirrors DW's selection rules
on the raw data so every row can carry a verdict: the twelve price filters of
`PriceService.FindPrices` (variant, unit, stock location, quantity threshold, language,
validity window, currency, country, shop, user / customer number, group / legacy customer
group), then *the cheapest matching row wins* — priority is not consulted, and ties fall to
database order (flagged). Visibility mirrors `AssortmentService`: a product in no assortment
is open to everyone (but hidden from assortment-filtered lists — flagged), otherwise the
account must hold one of its assortments through a user or group permission, or the
anonymous flag. Discounts are pre-checked on currency, shop, validity, language, country,
anonymous flag and user / group / customer-number targeting; cart-dependent conditions are
reported, not guessed. When DW's number and the mirrored selection disagree — a custom price
provider, a price subscriber — the report says so.

## Install

- **App store**: DW10 admin → Apps → Available apps → search "PowerTools" → install.
- **Package reference**: add `Truvio.Commerce.PowerTools` to your `Dynamicweb.Host.Suite` project.
- **Manual**: build, copy `Truvio.Commerce.PowerTools.dll` into the host's `bin\`, restart.

Access to the screens can be managed like any other permission: the app registers a
"Truvio PowerTools" permission entity with one key per tool
(`truvio-powertools-security-viewer`, `truvio-powertools-price-explainer`, `truvio-powertools-operations`, `truvio-powertools-search`). Per DW semantics
each is open until an admin explicitly manages it; checks fail closed.

## Development

```
dotnet build
dotnet test
```

Pack happens on build (`GeneratePackageOnBuild`). Publishing: tag `v*` → GitHub Actions
tests, packs, and pushes to NuGet via Trusted Publishing.
