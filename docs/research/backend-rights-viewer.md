# Backend Rights Viewer — research

> Proposed PowerTools **Security** section tool: *what rights does this backend user actually
> have in the admin UI, and why*. Covers both gates DW10 uses — classic **Permissions**
> (`PermissionSection`) and **Capability control** (the `Capabilities` feature flag) — plus
> the license gate.
>
> Status: **research only, no code written.** All type/method/table names below are verified
> against the decompiled 10.27.9 assemblies (`ilspycmd`), unless marked *unverified*.
> Source packages read: `Dynamicweb.Core`, `Dynamicweb.CoreUI`, `Dynamicweb.Application.UI`,
> `Dynamicweb.Insights.UI`, `Dynamicweb.Content.UI`, `Dynamicweb.Management.UI`.

---

## 0. The one rule that shapes the whole tool

`Dynamicweb.Application.UI.ShellScreen.GetAreas()` decides which areas a backend user sees:

```csharp
bool capabilityControlActive = CapabilityHelper.IsCapabilityControlActive();
foreach (AreaBase instance in AddInManager.GetInstances<AreaBase>())
{
    if (capabilityControlActive && instance.HasCapability())
    {
        if (instance.IsRestrictedForCurrentUser()) continue;   // capability decides
    }
    else if (!instance.GetPermissionSection().HasPermission(PermissionLevel.Read))
    {
        continue;                                             // permission decides
    }

    var licensable = TypeHelper.GetCustomAttribute<LicensableAttribute>(instance.GetType());
    if (licensable == null || LicenseManager.LicenseHasFeature(licensable.FeatureName))
        list.Add(instance);
}
return list.OrderBy(a => a.Sort).ToList();
```

**The two gates are mutually exclusive, per area.** Capability control does not layer on top of
permissions — when the flag is ON *and* the area declares a capability, the permission check is
skipped entirely. When the flag is OFF, or the area declares no capability, permissions decide
and capabilities are ignored. The license gate applies on top of whichever won.

This is the single most valuable thing the tool can show: administrators cannot see, in the
stock UI, *which* gate is deciding for a given area. The same either/or appears verbatim in
`PermissionExtensions.AddPermissionsContextAction` and `NavigationByPathQuery` (sections/nodes),
so it is a system-wide rule, not a shell quirk.

---

## (a) Capability-control mechanics — verified

### Feature flag

| Fact | Value |
|---|---|
| Feature type | `Dynamicweb.Core.CapabilityControl.CapabilityControlFeature : FeatureBase` |
| Name / Category | `"Capability Control"` / `"Capabilities"` |
| **Default** | `false` (OFF) |
| Read helper | `CapabilityHelper.IsCapabilityControlActive()` (`Dynamicweb.CoreUI.CapabilityControl`) |
| Equivalent | `Feature.IsActive<CapabilityControlFeature>()` (`Dynamicweb.Core`) |
| Persisted at | `/Globalsettings/Features/Capabilities/Capability Control` — `SystemConfigurationFeatureManager.SettingsPath(feature)` = `"/Globalsettings/Features/" + Category + "/" + Name` |
| Admin UI | Settings → Administration → Feature Management |

### Storage — deny-based, per user **group**

```sql
-- capabilities
SELECT * FROM CapabilityLimitation      -- CapabilityLimitationUserGroupId, CapabilityLimitationKey
-- capability sets
SELECT * FROM CapabilitySetLimitation   -- CapabilitySetLimitationUserGroupId, CapabilitySetLimitationKey
```

Both from `CapabilityRepository` / `CapabilitySetRepository` (internal, `Dynamicweb.CoreUI.CapabilityControl`).

Semantics worth spelling out in the UI, because they invert the mental model people bring from
permissions:

- A row is a **limitation (deny)**, not a grant. *No row = allowed.*
- Rows are keyed by **user group id only**. There is no per-user capability row, and no role rows
  — despite `CapabilityAccessDataModel.IsUserRolePermission` existing, `CapabilityScreenHelper`
  builds the owner-type editor with a single option `"User group"`, marked `Readonly`.
- The admin editor shows a binary state, not a level: `All` (unrestricted, `Icon.Unlock`,
  hint *"Has full control"*) vs `None` (restricted, `Icon.Times`, hint *"Has no rights"*).

### Evaluation — `DefaultCapabilityService.IsCapabilityLimitedForUser`

```csharp
// pseudo, from the decompile
bool limited(userId, key):
    if key.IsEmpty()                              -> false          // no capability = not limited
    if CapabilityHelper.GetCapabilityByKey(key) == null -> false    // unknown key = not limited
    if !CapabilityHelper.IsRelevantUser(userId)   -> false          // Angel / built-in admin bypass
    if !UserHasCapabilities(userId, capability.RequiredCapabilities) -> true   // parent cascade
    foreach groupId in UserManagementServices.UserGroups.GetGroupIdsByUserId(userId):
        if cache[groupId] contains key            -> true           // any group limits => limited
    return false
```

Consequences to surface:

- **Any one group restricting is enough** — group limitations are a union of denies, and
  `GetGroupIdsByUserId` is the flat membership list.
- **Required-capability cascade**: restricting `/Content` also restricts `/Content/Navigation`,
  because the child declares `RequiredCapabilities = [/Content]`. A user can be restricted from a
  child capability *with no row of its own* — the tool must explain that, or the report looks wrong.
- `CapabilityHelper.IsRelevantUser(userId)` returns `false` for `user.IsAngel` and
  `user.IsBuiltInAdmin` — those users are never capability-restricted. Note it does **not**
  exempt `IsAdmin` (the Administrator *user type*); ordinary administrators **are** subject to
  capability limitations. This differs from the permission side and is a real footgun.

### Arbitrary-user reads — fully supported, all public

| Need | API |
|---|---|
| Is user X restricted from capability K? | `CapabilityServices.Capabilities.UserHasCapability(int userId, CapabilityKey key)` → `false` means restricted |
| …several at once | `UserHasCapabilities(userId, IEnumerable<CapabilityKey>)` |
| Is node N restricted for user X? | `node.IsRestrictedForUser(int userId)` (`ActionExtensions`, public) |
| Does node N even have a capability? | `node.HasCapability()` (`ActionExtensions`) |
| Who is restricted from K? | `CapabilityServices.Capabilities.GetCapabilityAccesses([key])` → `HashSet<CapabilityAccess>` (`UserGroupId`, `Key`) — this is what the admin's own editor uses |
| One group + key | `GetCapabilityAccess(int userGroupId, CapabilityKey key)` |
| All declared capabilities | `CapabilityHelper.GetCapabilities()` → walks every `CapabilityProvider` add-in |
| One capability's metadata | `CapabilityHelper.GetCapabilityByKey(key)` → `Name`, `RequiredCapabilities` |
| Capability sets | `CapabilityHelper.GetCapabilitySets()`, `CapabilityServices.CapabilitySets.UserHasCapabilitySet(userId, setKey)` |

`ActionExtensions.IsRestrictedForUser(node, userId)` being public is the key enabler: **no
impersonation is needed on the capability side**, unlike permissions. It is a pure lookup.

The admin UI's own "restrict access" editor for reference/consistency:
`Dynamicweb.Application.UI.Screens.CapabilityControl.CapabilityAccessListScreen`, fed by
`CapabilityAccessesByKeyQuery { CapabilityKey = "..." }`
(`DataQueryListBase<CapabilityAccessDataModel, CapabilityAccess, CapabilityAccessListDataModel>`).
It is reachable only for `IsAngel || IsBuiltInAdmin` users — `CapabilityExtensions.AddCapabilityContextAction`
adds it as a context action labelled **"Permissions"** with `Icon.Lock` (confusingly, since it
edits capabilities).

### How components declare capabilities

`ActionNode.Capability` is a `CapabilityKey` (default `CapabilityKey.Empty`), `protected set`.
`AreaBase` overrides `TrySetCapability` to return `false`, so an area's capability can only be set
in its own constructor:

```csharp
public sealed class InsightsArea : AreaBase
{
    public InsightsArea()
    {
        Name = "Insights"; Icon = Icon.ChartLine; Sort = 10;
        SecondaryAction = NavigateScreenAction.To<DashboardOverviewScreen>()...;
        Capability = InsightsCapabilities.Area;      // "/Insights"
    }
}
```

Keys are hierarchical strings, compared **case-insensitively** (`CapabilityKey.Equals` uses
`StringComparison.OrdinalIgnoreCase`). Verified examples:

| Package | Keys |
|---|---|
| `Dynamicweb.Insights.UI` | `/Insights`, `/Insights/Dashboard`, `/Insights/Analytics`, `/Insights/Monitoring` |
| `Dynamicweb.Content.UI` | `/Content`, `/Content/Navigation`, `/Content/RecycleBin`, `/Content/Settings`, `/Content/Settings/Styles`, `/Content/Settings/ItemTypes` |

Note `/Content/Settings` declares **no** required capability (it is not a child of `/Content` for
cascade purposes) while `/Content/Navigation` does — the hierarchy in the *string* is not
authoritative; `Capability.RequiredCapabilities` is. **The tool must use `RequiredCapabilities`,
never string prefixes.**

Packages that ship a `CapabilityProvider` (binary grep for the type name, 10.27.9): `content.ui`,
`products.ui`, `ecommerce.ui`, `users.ui`, `files.ui`, `marketing.ui`, `integration.ui`,
`apps.ui`, `insights.ui`. **Not** shipped by: `application.ui` (Settings area), `global.ui`,
`management.ui`. So the Settings area is permission-gated even with the flag ON — a concrete,
demonstrable asymmetry the tool can show.

### Where capability filtering is actually applied

Two distinct layers, worth distinguishing in the report because they fail differently:

1. **Query/composition time** — `ShellScreen.GetAreas()` (areas), `NavigationByPathQuery`
   (sections/nodes: when the flag is ON and the node has a capability, `ProcessPermissions()` is
   skipped for it).
2. **Render time** — `ScreenBase.TrimContent()` walks `content.GetCapabilityAwareComponents()`
   and calls `ICapabilityAware.ProcessCapability()` on each, **only if the feature flag is
   active**. Implementors: `Shell` (filters `Areas`), `Tree` (filters `Areas`, `Sections`, each
   section's `Nodes`, context actions and action groups), `ScreenLayout` (filters toolbar
   actions), `ActionGroupExtensions.ProcessCapabilities`.

Both layers call the same `IsRestrictedForCurrentUser()`, so a single evaluation per node
reproduces both. There is no separate screen-level capability attribute — a screen is reachable
if the node/action that navigates to it survived.

---

## (b) Permission mechanics for backend areas — verified

### The entity

`AreaBase.GetPermissionSection()` returns `new PermissionSection(this.Name)` — **the area's
display name is the permission key**. `PermissionSection` is `[PermissionEntity("Section")]`, so
rows are stored with `Name = "Section"`, `Key = "<Area name>"`.

`PermissionSection` supports a **`/`-delimited hierarchy**: `GetPermissionParents()` splits the key
at the last `/` and yields the parent section. The constructor rejects keys starting or ending
with `/`. So a section key `"Content/Settings"` inherits from `"Content"`.

**Renaming an area silently orphans its permission rows** — a genuine finding the Content Access
Warnings engine could adopt (see §e).

### Levels

`PermissionLevel` (flags): `None=1`, `Read=4`, `Edit=0x14`, `Create=0x54`, `Delete=0x154`,
`All=0x554` (decimal 1, 4, 20, 84, 340, 1364 — matches the `dw-permission-model-facts` memory).
Areas require **`Read`** to appear. `PermissionExtensions` additionally strips `AddAction` without
`Create`, and clears `NodeAction` on nodes without `Read`.

`PermissionLevelExtensions.HasPermission` behaviour depends on a *second* feature flag,
`PermissionHierarchyFeature`: when active it is a plain `HasFlag`; when inactive, `None` short-
circuits everything and a match additionally requires the required level not to be `None`. The
tool should read this flag too, or its verdicts can disagree with the shell in edge cases.

### Resolution order — `PermissionManager`

For an entity, within a `PermissionContext`:

1. **Explicit** — a row on the entity itself, for any owner in the current priority level.
2. **Inherited** — walk `GetPermissionParents()` breadth-first; first ancestor level with any row wins.
3. **Default** — the first priority level where any owner declares a `DefaultPermission`.
4. Otherwise `context.DefaultPermission`.

Within one priority level, contributions from multiple owners are **merged with bitwise OR**
(`PermissionLevelExtensions.Merge` = `current | permission`) — i.e. **most permissive wins inside
a level**; the first level that produces *any* value stops the search.

### Owner priority (backend) — `PermissionOwnerPriorityManager.GetPermissionOwnerPriorityBackend`

```
level 0 : the user's direct groups          (User.GetGroups())
level 1 : those groups' parents             (UserGroup.GetParentGroup())
level n : ...up the group tree
last    : backend user roles                (UserRoleManager.GetUserRolesBackend)
```

Backend roles are exactly: `Administrator` (only if `user.IsAdmin`) and `AuthenticatedBackend`.
Their defaults:

| Role | `DefaultPermission` |
|---|---|
| `Administrator` | `All` |
| `AuthenticatedBackend` | **`null`** (no default) |
| `AuthenticatedFrontend` / `Anonymous` | `Read` (frontend only) |

**The user's own id is never a permission owner in the backend.** Per-user grants do not exist
here — same conclusion the Content Access Viewer already reached for render-time content.

Practical consequence, and the best single explanation the tool can give:

> A non-admin backend user with no group grants resolves to **nothing**: groups contribute no row
> and no default, `AuthenticatedBackend` has a `null` default, so the context default
> (`PermissionLevel.None`) stands. Backend access is grant-only. Conversely a user with the
> **Administrator user type** picks up the `Administrator` role's `All` default and sees every
> permission-gated area — which is why stock admins land on Insights.

### Bypasses / hard gates — `PermissionContext.BackendUserContext(User)`

```csharp
if (user == null || !user.GetAllowBackendWithInheritance())
    return BackendAnonymousContext();     // DefaultPermission = None, no owners => denies everything
if (user.IsBuiltInAdmin || user.IsAngel)
    return ElevatedContext();             // DefaultPermission = All => allows everything
return PriorityContext(GetPermissionOwnerPriorityBackend(user));
```

Three distinct verdict classes the report should name separately:

| Verdict | Cause | Detection |
|---|---|---|
| **No backend access at all** | `AllowBackend` false on the user *and* on every ancestor group | `user.GetAllowBackendWithInheritance() == false` |
| **Elevated (sees everything)** | `user.IsAngel` (`UserType.IsAngel()`) or `user.IsBuiltInAdmin` (username `admin` + Administrator/SystemAdministrator type) | user flags |
| **Administrator default** | `user.IsAdmin` (`UserType.IsAdministrator()`) → `Administrator` role default `All` | user flags |

Note the asymmetry vs capabilities: `IsAdmin` bypasses *permissions* (via the role default) but
**not** capabilities (`IsRelevantUser` only exempts Angel/built-in admin).

### Arbitrary-user evaluation recipe

`PermissionContext.BackendUserContext(User)` is **public** and disposable, and
`PermissionManager.GetPermissionLevel` honours whatever context is on the stack (it only
substitutes the current user when `PermissionContext.Current is DefaultPermissionContext`). So:

```csharp
// Evaluate an arbitrary backend user against an area, exactly as the shell would.
var user = UserManagementServices.Users.GetUserById(userId);

using (PermissionContext.BackendUserContext(user))
{
    var section = area.GetPermissionSection();
    PermissionLevel level = section.GetPermission();               // PermissionEntityExtensions
    bool visible          = section.HasPermission(PermissionLevel.Read);
}
```

Mirrors the `PermissionContext.FrontendUserContext(user)` pattern the Content Access Viewer
already uses (`dw-adminui-app-recipes` memory). **Contexts are an `AsyncLocal` stack and must be
disposed in reverse creation order** — `RemoveContext()` throws
`InvalidOperationException("Invalid permission context state…")` otherwise. Keep the `using` scope
tight and never `await` foreign code inside it.

Caveat: `BackendUserContext` branches on `ExecutingContext.IsBackEnd()` only in the
`UserContext(user)` wrapper — call `BackendUserContext` directly so the verdict is backend-shaped
regardless of where the code runs.

### Explicit-vs-inherited breakdown (for the Why? panel)

`PermissionService` (public, `Dynamicweb.Security.Permissions`) — context-free, ideal for
"who else is granted" tables:

| Method | Returns |
|---|---|
| `GetPermissionsByQuery(new PermissionQuery { Name = "Section", Key = "Content" })` | raw `Permission` rows (owner id, level) |
| `GetExplicitPermissionInfos(IPermissionEntity)` | `PermissionInfo` with `IsExplicit = true` |
| `GetInheritedPermissionInfos(IPermissionEntity)` | rows found by walking parents |
| `GetPermissionInfos(identifier)` | explicit ∪ inherited ∪ defaults, first-wins per owner |

`PermissionQuery.IncludeSubKeys = true` enumerates a whole section subtree in one call.

---

## (c) Feature-flag detection and graceful degradation

The tool must never *require* capability control. Behaviour matrix:

| Flag | Area declares capability | Deciding gate | What the tool shows |
|---|---|---|---|
| OFF | any | **Permission** | Permission column only; a muted note that capability control is off |
| ON | yes | **Capability** | Capability verdict as the decision; permission shown greyed as *"not consulted"* |
| ON | no | **Permission** | Permission decides; capability column shows *"— (no capability declared)"* |

Rules:

- Read the flag **once per report build** via `CapabilityHelper.IsCapabilityControlActive()`; do
  not call it per row (it resolves a DI service each time).
- With the flag OFF, still surface any rows present in `CapabilityLimitation` as an **info-level
  finding**: *"3 capability limitations are stored but capability control is off — they have no
  effect today and will take effect the moment the flag is enabled."* That is exactly the kind of
  latent-config surprise PowerTools exists to catch.
- Fail **closed and quiet**: wrap capability calls in try/catch and degrade the column to
  "unknown" rather than failing the screen — matching `PowerToolsAccess.HasLevel`'s existing
  fail-closed convention.
- Guard the whole tool behind the existing `PowerToolsPermissionEntity.SecurityViewerKey` grant
  (or a new `truvio-powertools-backend-rights` key — see §d).

---

## (d) Proposed screen design

Fits the established Security-section shape: **pick an account → report**, mirroring
`ExplainerAccountListScreen → ProductPickScreen → PriceExplainScreen`.

### Navigation

Add to `SecurityNodeProvider.GetRootNodes()` (Security section, `Sort = 10`), after the existing
two nodes:

```csharp
public const string BackendRightsNodeId = "PowerTools_BackendRights";   // no '/' allowed

yield return new NavigationNode
{
    Id = BackendRightsNodeId,
    Name = "Backend Rights Viewer",
    Icon = Icon.UserCircle,        // exists at the 10.8.4 floor; Icon.Lock also available
    Sort = 30,
    HasSubNodes = false,
    NodeAction = NavigateScreenAction.To<BackendRightsListScreen>().With(new BackendRightsListQuery())
};
```

Permission key: reuse `SecurityViewerKey` for a minimal diff, **or** add
`truvio-powertools-backend-rights` to `PowerToolsPermissionEntity.AllKeys`/`ToolKeys`. Recommend
the latter — this tool exposes who-can-do-what across the whole backend, which is more sensitive
than the content viewer, and `PowerToolsPermissionEntityLookup` already resolves any key in
`AllKeys` with no extra work.

### Screens

| Screen | Base | Query | Purpose |
|---|---|---|---|
| `BackendRightsListScreen` | `ListScreenBase<BackendUserModel>` | `BackendRightsListQuery` | Step 1: pick a backend user (or group). Lists only users where `GetAllowBackendWithInheritance()` is true, plus a *"Show users without backend access"* toggle. Searchable via `[ConfigurableProperty(..., isSearchable: true)]` + `DataQueryListBase` |
| `BackendRightsScreen` | `OverviewScreenBase<BackendRightsModel>` | `BackendRightsQuery { UserKey, ShowAllCapabilities }` | Step 2: the report |
| `BackendRightsWhyScreen` | slide-over (`OpenSlideOverAction`) | `BackendRightsWhyQuery { UserKey, AreaType }` | "Why?" for one area |

`OverviewScreenBase` is mandatory for the report — `ListScreenBase` splits width evenly and clips
long text, and these explanations are long text (`dw-adminui-app-recipes` memory, verified 10.27.9).

`BackendRightsListQuery` should reuse `DwAccountCatalog.GetUsers(search, pageSize)` and add a
backend filter; `SecurityAccount` already models `user:17` / `group:42` keys and round-trips
through screen queries, so **reuse `AccountKey`** rather than inventing a new key format. One
caveat: `SecurityAccount.OwnerIds` is built for *frontend* resolution (AuthenticatedFrontend +
groups). For backend the owner set differs (groups + ancestors + `AuthenticatedBackend` +
`Administrator`), so the backend evaluator must compute its own owner list — do **not** reuse
`OwnerIds` here. This is the main correctness trap in the whole design.

### Report layout (`BackendRightsScreen`)

**Info bar** (`SetInfobar`, `InfoBar` + `CardInfo.InfoValue`, per the Price Explainer):

| Field | Value |
|---|---|
| User | `Jane Doe (id 3074)` |
| Backend access | `Badge` Yes/No — `GetAllowBackendWithInheritance()` |
| Effective status | `Elevated` / `Administrator` / `Standard` / `No access` |
| Areas visible | `7 of 12` |
| Gate in force | `Capabilities (feature on)` / `Permissions` |

**Section 1 — Admin areas** (`HtmlBlock` table, `Group.GroupWidth.Col_12`), one row per
`AddInManager.GetInstances<AreaBase>()` ordered by `Sort`:

| Column | Content |
|---|---|
| Area | `Name` (+ `Sort`, muted) |
| Sees it | `Badge` Yes / No |
| Decided by | `Badge` Capability / Permission / License / Bypass |
| Capability | key + `Allowed`/`Restricted`/`— none declared`; greyed when the flag is off |
| Permission | resolved `PermissionLevel` name + `explicit` / `inherited from 'X'` / `default`; greyed when capability decided |
| License | `LicensableAttribute.FeatureName` + ok/missing, or `—` |
| Why? | link opening the slide-over |

Reuse `Badges.Level` / `Badges.Origin` / `Badges.Visible` from `AdminUI/Badges.cs` where the shapes
fit. Remember: **a `Badge` carrying an `Icon` renders icon-only and drops `Value`** — keep these
text-only.

**Section 2 — Capability limitations** (only when the flag is ON, or when rows exist while OFF —
then headed *"Stored but inactive"*): every capability the user is restricted from, with the
group(s) causing it and whether it is direct or cascaded via `RequiredCapabilities`.

**Section 3 — Group membership & priority**: the resolved backend owner priority, in order —
direct groups, ancestor groups level by level, then roles — with each owner's `DefaultPermission`.
This is the "why does the wrong group win" answer, and nothing in the stock UI shows it.

**Section 4 — Findings** (reuse `Core/Diagnostics/Finding.cs` + `IWarningRule`, so rows can also
surface in Content Access Warnings). Candidate rules in §e.

### Why? slide-over wording

Follow `AccessExplanation`'s voice — name the winner, say why membership doesn't help, name who
*does* get in. Templates:

- Capability, restricted directly:
  *"Capability control is on and 'Insights' requires `/Insights`. Group 'Editors' restricts it, so the area is hidden. Removing that limitation, or removing Jane from 'Editors', restores it."*
- Capability, cascaded:
  *"'Navigation' requires `/Content`, which group 'Editors' restricts. There is no limitation on `/Content/Navigation` itself — it is hidden because its parent capability is."*
- Capability active, permission irrelevant:
  *"'Content' declares capability `/Content` and capability control is on, so section permissions are not consulted for this area — the Read grant on section 'Content' has no effect today."*
- Permission, no grant:
  *"Section 'Commerce' has no permission for any of Jane's groups, and 'Authenticated users (backend)' declares no default. Backend access is grant-only, so the area is hidden. 'Sales' and 'Support' are granted Read."*
- Permission, inherited:
  *"Section 'Content/Settings' has no row of its own; it inherits Read from section 'Content' (granted to 'Editors')."*
- Elevated:
  *"Built-in administrator — bypasses every permission check. Note: capability limitations are also skipped for this user."*
- Administrator type:
  *"User type Administrator grants the 'Administrators' role a default of All, which satisfies every section. Capability limitations still apply to this user."*

---

## (e) Risks, unknowns, and live verification checklist

**Needs live verification (marine-demo 10.28.6 or the cabp host 10.27.9):**

1. **Does the tool's verdict match the shell?** Ground truth: log in as a restricted persona
   (`pim.editor` on marine-demo is documented as genuinely restricted — no Settings area) and
   compare the rendered area tabs against the report for that user. This is the acceptance test.
2. **`CapabilityLimitation` / `CapabilitySetLimitation` existence.** Both tables are referenced by
   internal repositories; confirm they exist on 10.27.9 *and* 10.28.x, and whether they are created
   by an update provider or ship in the base schema. If a solution never enabled the feature, the
   tables may be absent → wrap the read and degrade to "capability data unavailable".
3. **`PermissionHierarchyFeature` state** on target solutions — it changes `HasPermission`
   semantics. Read it and, if the two interpretations disagree for a row, say so rather than
   picking one.
4. **Sub-areas.** `AreaBase.SubAreas` (`NavigationArea`) are filtered by `ManipulateArea` in
   `NavigationByPathQuery`; whether they carry their own capability/permission gate is
   **unverified**. Products/Commerce use sub-areas. Scope v1 to top-level areas + sections + nodes
   and state the limitation.
5. **Section/node capability coverage.** `NavigationSection`/`NavigationNode` inherit
   `ActionNode.Capability`, but which stock sections actually set one was not enumerated. Affects
   how deep the report can go below area level.
6. **`LicensableAttribute` namespace** (`Dynamicweb.CoreUI.License` per `ShellScreen`'s usings) and
   `LicenseManager.LicenseHasFeature` — *not read in this pass*. The 10.28 floor also made the old
   `Licensable(bool)` ctor obsolete-as-error (memory). Verify before relying on the License column.
7. **Cost.** `PermissionContext` + a permission resolve per area per user is fine for one user
   (~12 areas); a *matrix* view over all backend users would be O(users × areas) with cache
   lookups each. Keep v1 single-user; if a matrix is added, batch via
   `GetPermissionsByQuery(new PermissionQuery { Name = "Section" })` once and resolve in memory.

**Known traps already resolved (do not re-litigate):**

- Do not reuse `SecurityAccount.OwnerIds` for backend evaluation (frontend-shaped) — §d.
- Do not derive capability cascade from key string prefixes — use `RequiredCapabilities` — §a.
- Capability rows are **denies**, keyed by **group only**; no user rows, no role rows — §a.
- `PermissionContext` must be disposed in reverse order within the same async flow — §b.
- Node ids must not contain `/` (`NavigationNodePath` splits on it) — §d.
- Every public property of a `DataQueryModelBase` is serialised into the screen URL; expose
  computed values as methods, not properties.

**Candidate warning rules** (fold into `Core/Diagnostics`, surfaced in Content Access Warnings):

| Rule | Severity | Detection |
|---|---|---|
| Capability limitations stored while the feature is off | Info | rows exist ∧ `!IsCapabilityControlActive()` |
| Limitation references an unknown capability key | Warning | `GetCapabilityByKey(key) == null` — orphaned by an uninstalled app |
| Limitation references a deleted group | Warning | `GetGroupById` returns null |
| Section permission key matches no live area | Warning | `Name="Section"` row whose `Key` is not an area/section name — the rename-orphan case (§b) |
| Backend user with backend access but zero visible areas | Warning | evaluator reports 0 of N — user can log in and see nothing |
| Area gated by capability while section permissions are also configured | Info | both configured, only one consulted — dead configuration |

---

## Appendix — verified API quick reference

```csharp
// Feature flag
bool on = CapabilityHelper.IsCapabilityControlActive();          // Dynamicweb.CoreUI.CapabilityControl
bool on2 = Feature.IsActive<CapabilityControlFeature>();          // Dynamicweb.Core[.CapabilityControl]

// Capabilities (no impersonation needed)
IReadOnlyCollection<Capability> all = CapabilityHelper.GetCapabilities();
Capability? cap  = CapabilityHelper.GetCapabilityByKey(new CapabilityKey("/Content"));
bool allowed     = CapabilityServices.Capabilities.UserHasCapability(userId, cap.Key);
HashSet<CapabilityAccess> denies = CapabilityServices.Capabilities.GetCapabilityAccesses([cap.Key]);
bool restricted  = actionNode.IsRestrictedForUser(userId);        // ActionExtensions
bool declares    = actionNode.HasCapability();

// Areas
IEnumerable<AreaBase> areas = AddInManager.GetInstances<AreaBase>();   // order by .Sort
PermissionSection section   = area.GetPermissionSection();             // key == area.Name

// Permissions for an arbitrary backend user
using (PermissionContext.BackendUserContext(user))
{
    PermissionLevel level = section.GetPermission();
    bool canSee           = section.HasPermission(PermissionLevel.Read);
}

// Permission rows, context-free
var svc  = new PermissionService();
var rows = svc.GetPermissionsByQuery(new PermissionQuery { Name = "Section", Key = "Content" });
var expl = svc.GetExplicitPermissionInfos(section);
var inh  = svc.GetInheritedPermissionInfos(section);

// User gates
bool backend  = user.GetAllowBackendWithInheritance();
bool elevated = user.IsAngel || user.IsBuiltInAdmin;   // bypasses permissions AND capabilities
bool adminTyp = user.IsAdmin;                          // bypasses permissions only
```
