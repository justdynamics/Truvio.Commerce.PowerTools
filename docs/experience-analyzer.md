# Experience Analyzer

**PowerTools ▸ Security ▸ Experience Analyzer** — read-only. Answers *what stands out about what
this account can see, and where do two accounts differ?*

Permission: `truvio-powertools-security-viewer` (Read) — the same grant as the Content Access
Viewer, because it is the same data seen from the other end. The Content Access Viewer walks one
account down the whole page tree; the analyzer skips the walk and reports only what is
**different**, either against the public baseline or against a second account.

## The question it exists for

Role-specific content is configured by granting pages to groups: the Lumber dealers get their
dashboard, the Roofing dealers get theirs. Verifying that on the frontend means signing in as each
role and clicking around, and the answer is only ever "it looked right". The analyzer states it
directly: *these are the pages only Lumber sees, these are the pages only Roofing sees, and here is
the rule gating each one.*

## Screens

| Node | Screen | What it answers |
|---|---|---|
| Experience Analyzer | `ExperienceAnalyzerScreen` | One account against the public baseline, or two accounts side by side |

An overview screen, not a list grid: every difference carries a gate explanation, and the grid gives
each column equal width and clips long text.

### The report

**Info bar** — account (and the compared account), website scope, how many pages each side sees, and
the number of differences as a badge. `None` in green means the two experiences are identical, which
is itself an answer.

**Summary** — one sentence: how many pages tell the two apart, how many are shared, how many are
hidden from both.

**Websites** — per website: total pages, how many each side sees, and whether the website carries any
difference at all. On a multi-site solution this is where you see that only one site is
role-sensitive.

**Only *A* sees / Only *B* sees** — the standouts. Website, page path, an *Audit* link into the
Content Access Viewer's page drilldown for the side that is denied, and the explanation of why that
side does not get in ("Gated here: 'Authenticated frontend role' is set to None and 'Roofing' has no
grant of its own. Only 'Lumber dealers' can see it."). Each section lists at most 100 pages and says
how many were left out.

### Single-account mode

With no comparison account picked, the **anonymous role is the baseline**, so a single account still
has something to stand out against:

- **Exclusive to this account** — pages gated to it and hidden from the public. This is what its
  group membership earns it.
- **Hidden from this account, public sees it** — a signed-in account seeing *less* than the public.
  Rarely intended; usually a `None` grant that outranks nothing, or a group grant that replaced an
  inherited one. The summary calls this out explicitly when it occurs.

Picking the anonymous role itself as the account produces the tallies and a note, since a baseline
cannot stand out against itself.

## Demo walkthrough

1. Configure the role pages as normal — grant the Lumber dashboard to the Lumber group, the Roofing
   dashboard to the Roofing group, deny the broad role on both.
2. Verify on the frontend that each role lands on its own dashboard.
3. Open **Experience Analyzer**, pick the Lumber group, then **Compare with…** and pick the Roofing
   group.
4. The report names it: *Only Lumber Co. sees → Home / Lumber dashboard*, *Only Roofing &
   Restoration sees → Home / Roofing dashboard*, with the gate behind each. The website table shows
   the difference count per site, and the *Audit* link opens the denied side's page drilldown when a
   result needs unpacking.
5. **Stop comparing** returns to single-account mode against the public baseline — useful for
   checking that a role has not accidentally lost public content.

## What it deliberately does not do

- **It does not render or preview pages.** No page is fetched, no template is executed, nothing is
  screenshotted. The report is about *access*.
- Because of that, it explains what DW's permission model decides — a template that hides a section
  for its own reasons (an empty query, a personalisation rule inside the page, a paragraph the
  frontend suppresses) is outside its scope. A page can be visible here and still look empty to the
  visitor.
- It reports **pages**. Grid rows and paragraphs resolve per page in the Content Access Viewer's page
  drilldown, one *Audit* click away.
- It never writes. Nothing here changes a permission.

## Verified facts

- Page verdicts come from `EffectiveAccessEvaluator`, the same engine behind the Content Access
  Viewer, so a verdict here and a verdict there can never disagree.
- Explanations come from `AccessExplanation`, so the wording matches the viewer's ("Gated here: …
  Only 'X' can see it").
- Comparison and bucketing live in `Core/Permissions/ExperienceComparison.cs` — pure, no Dynamicweb
  types, unit-tested against hand-built page sets including the baseline cases and the row cap.
- Accounts resolve through `SecurityAccount` keys (`role:Anonymous`, `group:42`, `user:17`), so every
  state of the screen is a shareable URL.
- Per-user permission rows are ignored by DW at render time; a user account therefore resolves
  through its groups plus the authenticated frontend role, exactly as the runtime does.
- The two account pickers use **two distinct pick tokens** — one store entry per dimension, or the
  second pick would overwrite the first.

## Verified on

DW 10.27.9 (cabp). Builds against the 10.8.4 floor and 10.27.9.
