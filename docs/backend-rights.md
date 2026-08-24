# Backend Rights Viewer

**PowerTools ▸ Security ▸ Backend Rights Viewer** — read-only. One place that answers *what can this
backend user actually see in the administration, and which gate decided it?*

Permission: `truvio-powertools-backend-rights` (Read). Its own grant, deliberately not folded into
the Content Access Viewer's: this report exposes who-can-do-what across the whole backend, which is
more sensitive than content visibility.

Dynamicweb 10 gates the admin with **two** independent mechanisms — classic **section permissions**
and **capability control** — plus the license. Nothing in the stock UI tells you which one is
deciding for a given area, and the two are *mutually exclusive*, not layered. That is the gap this
tool fills.

## The rule that shapes everything

`ShellScreen.GetAreas()` (verified, 10.27.9 decompile):

```csharp
if (capabilityControlActive && area.HasCapability())
{
    if (area.IsRestrictedForCurrentUser()) continue;   // capability decides
}
else if (!area.GetPermissionSection().HasPermission(PermissionLevel.Read))
{
    continue;                                         // permission decides
}
// license applies on top of whichever won
```

| Capability control | Area declares a capability | Deciding gate | What the report shows |
|---|---|---|---|
| OFF | any | **Permission** | Permission column only; the declared capability is noted as *not enforced* |
| ON | yes | **Capability** | Capability decides; the permission is shown greyed as *not consulted* |
| ON | no | **Permission** | Permission decides; capability column reads *— none declared* |

With the flag ON, a granted Read on a capability-gated area is **dead configuration**. The report
says so rather than showing a permission that no longer does anything.

### The three depths gate differently

Full depth (areas, sections and nodes) is reported, and the three levels genuinely differ:

| Level | Source | Gate |
|---|---|---|
| **Area** | `ShellScreen.GetAreas()` | The either/or above. |
| **Section** | `NavigationByPathQuery.GetSectionResult` | Permissions are consulted **only while the flag is OFF**, and even then `ProcessPermissions()` filters the section's *child nodes and context actions*, never the section itself. With the flag ON a section that declares no capability is gated by nothing at composition time — only `ShouldShow()` and render-time capability filtering remain. |
| **Node** | `GetNodeResult` | Same either/or as areas, plus: a node is dropped when the parent's level does not satisfy the node's own `PermissionLevelRequired` (default Read). |

A section has **no permission entity of its own**. `NavigationSection`'s constructor sets
`PermissionLevelCurrentUser` from its *area's* permission section, so the report shows the area's
level on section rows — that is what DW itself uses.

## Verdict classes

| Verdict | Cause | Consequence |
|---|---|---|
| **No access** | `GetAllowBackendWithInheritance()` is false | Cannot sign in; nothing below matters |
| **Elevated** | `IsAngel` or `IsBuiltInAdmin` | Bypasses **both** gates |
| **Administrator** | `IsAdmin` (the user *type*) | Bypasses permissions via the `Administrator` role's `All` default — **but not capabilities** |
| **Standard** | everything else | Grant-only: no grant means nothing is visible |

That third row is the real footgun, and the report names it explicitly: an ordinary administrator
*is* subject to capability limitations.

**Backend access is grant-only.** The `AuthenticatedBackend` role declares a `null` default, so a
non-admin with no group grants resolves to nothing and sees an empty shell. `SECOPS-B5` reports
exactly that situation.

## Capability limitations are denies, per group

- A row in `CapabilityLimitation` is a **deny**, not a grant — *no row means allowed*.
- Rows are keyed by **user group only**. There is no per-user and no per-role row.
- **Any one** of the user's groups carrying the row is enough.
- The cascade follows `Capability.RequiredCapabilities`, **never the key string**. DW ships
  `/Content/Navigation` requiring `/Content`, while `/Content/Settings` requires nothing — so
  restricting `/Content` hides Navigation but leaves Settings alone, however nested the keys look.

The report separates **Direct** from **Cascaded** and names the group behind each, because a
cascaded restriction has no row of its own and otherwise reads as a bug.

## Screens

| Screen | Base | What it does |
|---|---|---|
| `BackendRightsListScreen` | `ListScreenBase` | Pick the user. Lists accounts that can reach the admin; an action toggles the rest into view |
| `BackendRightsScreen` | `OverviewScreenBase` | The report: info bar, areas tree, capability limitations, owner priority, findings |
| `BackendRightsWhyScreen` | slide-over | One row explained in full, in the Content Access Viewer's voice |

Report tables are `HtmlBlock` inside an overview screen, not the list grid: the grid gives every
column equal width and never wraps, and these are explanations.

## Rules

Pure over a `RightsSnapshot`, in `Core/Rights/Rules/`. They also surface on **Content Access
Warnings** beside the `SECOPS-W` rules, minus `SECOPS-B5` which is per-user and belongs on the report.

| Rule | Severity | Catches |
|---|---|---|
| SECOPS-B1 | Info | Limitations stored while capability control is off — no effect today, full effect the moment it is switched on |
| SECOPS-B2 | Warning | A limitation on a key no installed provider declares (typically left by an uninstalled app); it limits nobody |
| SECOPS-B3 | Warning | A limitation owned by a deleted user group |
| SECOPS-B4 | Warning | Section permission rows matching no live area — a section's key is the area's **display name**, so renaming an area orphans its rows silently |
| SECOPS-B5 | Warning | A user with backend access but zero visible areas: signs in to an empty shell |
| SECOPS-B6 | Info | An area gated by capability while section permissions are also configured — only one is consulted |

Suppress any of them with `SuppressedWarningRules` in PowerTools settings; suppression is never
silent, the hidden count is always shown.

## How an arbitrary user is evaluated

Capabilities need **no impersonation** — `UserHasCapability(userId, key)` and
`IsRestrictedForUser(node, userId)` are public lookups.

Permissions do. The tool enumerates the whole admin tree inside one tight scope:

```csharp
using var context = PermissionContext.BackendUserContext(user);
// areas, sections and nodes are built HERE, so DW's own permission resolution
// (NavigationSection's constructor included) answers for the target user
```

`PermissionContext` is an `AsyncLocal` stack that must be disposed in reverse creation order, so
nothing inside that scope awaits or calls foreign async code.

## Version behaviour

Capability control ships with **Dynamicweb 10.19**; the suite's floor is 10.8.4. Binding to
`Dynamicweb.CoreUI.CapabilityControl` at compile time would make the assembly unloadable on an older
host — and DW's AddInManager skips such an assembly *silently*, taking every other PowerTools screen
with it. So every capability read goes through `CapabilityReflection`, resolved once and cached.

On a host without the API the report degrades to *"capability data unavailable"*, states that all
verdicts come from section permissions, and keeps working. The same late binding covers
`PermissionLevelCurrentUser`, `PermissionLevelRequired`, `PermissionHierarchyFeature` and
`LicensableAttribute`, all of which are newer than the floor or have shifted shape across versions.

Cross-check: DW's own `UserHasCapability` answer is captured beside the tool's own computation. When
they disagree the report **says so** instead of picking a side — a disagreement means something is
holding state the report cannot see (typically a stale capability cache).

## Verified against

- Dynamicweb 10.27.9 decompile for every type, method and gate order cited here.
- Capability-control presence bisected across packages: **absent at 10.18.11, present at 10.19.6**.
- Builds clean at `-p:DynamicwebVersion=10.8.4` and `10.27.9`.

Still to confirm on a live host: verdicts against a genuinely restricted persona; whether
`CapabilityLimitation` exists on a solution that never enabled the feature; and sub-area gating
(`AreaBase.SubAreas`, used by Products and Commerce), which this version does not descend into.


## Verified live (cabp, DW 10.27.9, 2026-08-24)

Acceptance test against the shell, both flag states, persona `pmerritt` (frontend buyer given
backend access + a single group grant of Read on section "Users"):

- **Flag OFF**: shell shows exactly one tab (Users); the report says Standard, Users = Yes
  (Read, explicit via group), every other area No (no grant), and surfaces the stored
  capability deny as "(not enforced)". 1:1 match.
- **Flag ON** (+ `/Users` limitation on the user's group): the shell flips to EIGHT areas
  (Insights, Content, Assets, Products, Commerce, Email, Integration, Apps) and hides Users —
  the report reproduces every row: capability-declaring areas Yes/"Allowed" with the permission
  column marked *not consulted*, Users No/"Restricted by <group>" with the user's own Read
  grant marked *not consulted* (dead config), PowerTools + Settings still permission-gated.
- The elevated verdict (built-in admin: every row "Bypass") and the full areas → sections →
  nodes depth render as designed.

Operational gotcha found on the way, worth knowing when demonstrating: the feature's
GlobalSettings node must be the **space-stripped** name (`<Capabilities><CapabilityControl>`)
— DW's config reader strips spaces from lookup keys but indexes the file by raw node names, so
a hand-edited, XML-encoded `Capability_x0020_Control` node is never read, and a Feature
Management toggle takes effect in-process immediately but the area composition is only fully
re-evaluated on fresh navigation.
