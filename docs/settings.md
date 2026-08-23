# PowerTools settings

**PowerTools ▸ Settings ▸ PowerTools settings** — the one screen in the suite that writes.
Every other tool is strictly read-only; this screen writes nothing but its own keys, all of them
under `/Globalsettings/Truvio/PowerTools/`. Deleting that single node from
`Files/GlobalSettings.config` resets the whole product to its shipped behaviour.

Permission: **any** PowerTools function grant with Read opens the screen; **Edit** on the new
`truvio-powertools-settings` grant is what turns it from a read-only view into an editable form.

## How it is built (DW's own settings pattern)

| Piece | Type | What it does |
|---|---|---|
| `AdminUI/Models/PowerToolsSettingsModel.cs` | `SettingsViewModelBase` | Every property carries `[Settings(path, default)]` + `[ConfigurableProperty(label, hint)]`. The base constructor calls `SettingsService.Load(this)`, which fills the properties from GlobalSettings. |
| `AdminUI/Queries/PowerToolsSettingsQuery.cs` | `DataQueryModelBase<…>` | `GetModel()` is just `new PowerToolsSettingsModel()` — constructing it *is* the load. It also sets `PermissionLevelCurrentUser`. |
| `AdminUI/Screens/PowerToolsSettingsScreen.cs` | `EditScreenBase<…>` | Groups the editors into five tabs; `GetEditor` swaps in `Textarea` / `Number` (with an `Append` unit) where the default editor is wrong. |
| `AdminUI/Commands/PowerToolsSettingsSaveCommand.cs` | `CommandBase<…>` | Re-checks Edit, then `SettingsService.Persist(model)`. |
| `Core/Settings/PowerToolsSettingKeys.cs` | — | Every key path and every shipped default, as `const`s (attributes need compile-time constants). |
| `Core/Settings/PowerToolsSettings.cs` | pure record | What the tools actually read: parsing, matching and finding-suppression rules, unit-tested. |
| `Core/Settings/Dw/DwPowerToolsSettings.cs` | Dw adapter | `Current` — reads the keys out of `SystemConfiguration.Instance`, defaults in memory. |

This is a straight copy of how DW builds its own settings screens (decompiled at 10.8.4:
`LogRetentionSettingsDataModel` / `LogRetentionSettingsQuery` / `LogRetentionSettingsEditScreen` /
`LogRetentionSettingsSaveCommand` in `Dynamicweb.Application.UI`).

### DW facts verified by decompiling (10.8.4)

* `Dynamicweb.Extensibility.Settings.SettingsAttribute` and `SettingsService` live in
  **`Dynamicweb.Core.dll`** (not in `Dynamicweb.dll`, and not documented in any `.xml`).
  `SettingsService.Persist(object)` reflects over the `[Settings]` properties and calls
  `SystemConfiguration.Instance.SetValue(path, value)` for each. `SettingsService.Load(object)`
  is the inverse and understands `string`, `int`/`long` (+ nullable), `double` (+ nullable),
  `bool`, enums and `Convert.ChangeType` for everything else.
* `Dynamicweb.Configuration.ConfigurationManager.SetValue<T>` **persists immediately** — it calls
  `provider.Persist()` itself and raises `DWN_STANDARD_CONFIGURATION_SET`. There is no separate
  `Save()` to call after a `SetValue`.
* `SettingsService.Load` also *writes*: `EnsureDefaultValue` stores the attribute's default into
  the configuration file whenever the key is missing. That is fine for a settings screen, but
  wrong for a read path that runs on every render — which is why `DwPowerToolsSettings` reads the
  keys directly and falls back **in memory** instead of using `SettingsService.Load`.
* `Dynamicweb.Configuration.XmlConfigurationProvider` keeps the parsed configuration in a
  `ConcurrentDictionary` and `SystemConfiguration.Instance` is a process-wide singleton, so a read
  is a dictionary lookup. No caching layer was added — it would only add staleness.
* Values are written as XML **element text**, and an empty value round-trips as a whitespace-only
  text node. `DwPowerToolsSettings` therefore treats whitespace-only as "unset".
* `Dynamicweb.CoreUI.Data.SettingsViewModelBase` is in `Dynamicweb.CoreUI.dll` (undocumented), and
  `DataViewModelBase` **throws** unless the type name ends in `Model`.
* `EditScreenBase.AddSaveButtons` / `GetSubmitAction` render Save only when
  `Model.PermissionLevelCurrentUser` has `PermissionLevel.Edit` (20) — that is the supported way to
  make an edit screen read-only, and DW's own comment on the property says the command must still
  check for itself.
* `Icon.Cog` exists at the 10.8.4 floor (`Icon.Cogs` does not).

## The settings

### Query linter (`/Globalsettings/Truvio/PowerTools/Search/…`)

| Setting | Key | Default | Effect |
|---|---|---|---|
| Ignored rule ids | `IgnoredRules` | — | Findings with these rule ids never show. Trailing `*` matches a prefix. |
| Ignored query parameters | `IgnoredParameters` | — | Drops IDX-W1/IDX-W2 findings *about* those parameters. |
| Ignored queries | `IgnoredQueries` | — | Drops every finding on those queries (bare name, `Repository/Item` key, or the `Name (Repository)` display form). |
| Stale index after | `StaleIndexHours` | 24 | `DwSearchSource` marks an index Stale past this age; feeds IDX-W17. |
| Document row cap | `DocumentRowsPerPage` | 50 | Hard ceiling on documents the document browser reads at once (itself capped at 500). |

### Operations (`…/Operations/…`)

| Setting | Key | Default | Effect |
|---|---|---|---|
| Stale task tolerance | `StaleTaskIntervalMultiplier` | 2 | Intervals a repeating task may miss before OPS-W2. |
| Log folder warning size | `LogFolderWarningMb` | 500 | OPS-W6 fires at or above this. |
| Log folder critical size | `LogFolderCriticalMb` | 2048 | OPS-W6 becomes Critical above this. |
| Table share warning | `TableSharePercent` | 25 | OPS-W8 share threshold. |
| Recent changes window | `RecentChangesDays` | 30 | Default window when the screen is opened without an explicit `Days`. |
| Run history depth | `RunHistoryDepth` | 20 | Runs listed on a task's detail screen. |

### Price Explainer (`…/Commerce/…`)

| Setting | Key | Default | Effect |
|---|---|---|---|
| Product picker cap | `ProductPickCap` | 200 | Products per search in the picker. |
| Price row cap | `PriceRowCap` | 100 | Price-matrix rows rendered; the rest collapse into a "N further row(s) not shown" line. |
| Quantity presets | `QuantityPresets` | `1,5,10,25,50,100,500` | The quantity context switches. |
| Date presets | `DatePresetDays` | `7,30,90` | The `+N days` context switches ("now" is always offered). |
| Default currency | `DefaultCurrencyCode` | — | Used when an explanation names no currency; blank keeps DW's default. |

### Content Access (`…/Security/…`)

| Setting | Key | Default | Effect |
|---|---|---|---|
| User fetch cap | `UserFetchCap` | 500 | Users materialised per request in both account pickers. |
| Suppressed warning rules | `SuppressedWarningRules` | — | SECOPS rule ids never shown (trailing `*` works). |
| Hide administrator accounts | `HideAdministrators` | false | Administrators bypass every check, so their rows explain nothing. |

### General (`…/General/…`)

`SecuritySectionEnabled`, `CommerceSectionEnabled`, `OperationsSectionEnabled`,
`SearchSectionEnabled` (all default true) hide a whole section from the PowerTools area — they are
ANDed into each section's `ShouldShow()`, so the permission still governs and the toggle only ever
takes away. `ShowRuleIds` (default true) drops the rule id column from both finding lists.

## Suppression is never silent

Both finding lists append one last row when settings hid something:

> **PowerTools settings — 6 findings hidden by settings** · *Ignored rule ids, parameters and queries are configured under PowerTools ▸ Settings.*

That row carries no severity, so it gets no badge, and both screens have a **PowerTools settings**
action that jumps straight to this screen.

The parameter suppression is precise rather than text-matched: `Finding` gained an optional
`Subject` field, and IDX-W1 fills it with the parameter it is about while IDX-W2 fills it with the
comma-separated parameters whose missing defaults collapse the query. An IDX-W2 finding disappears
only when **every** parameter behind it is ignored.

## Parsing rules

`PowerToolsSettings.SplitList` splits on `,` `;` `|` newline, tab and space, trims, drops blanks
and de-duplicates case-insensitively — a textarea, a comma list and a whitespace-folded XML value
all parse the same way. Numeric settings go through `PowerToolsSettings.Positive(value, fallback)`,
so a stored `0` or negative falls back to the shipped default instead of disabling a tool. The
threshold-bearing rules (`StaleTaskRule`, `LogGrowthRule`, `TableBloatRule`) validate their own
constructor arguments the same way, so `new OperationsHealthEngine(PowerToolsSettings.Defaults)`
produces byte-identical findings to `new OperationsHealthEngine()`.

## Verified on cabp (DW 10.27.9)

* The screen renders all five tabs, saves, and reloads with the saved values; the keys land under
  `<Globalsettings><Truvio><PowerTools>…` in `Files/GlobalSettings.config`.
* Ignoring parameters `eq, q` took the Query linter from **16 findings to 10**, with a
  "6 findings hidden by settings" row.
* Ignoring rules `IDX-W1` + `IDX-W4` took it to **1 finding** with "15 findings hidden by settings".
* Turning **Show rule ids** off removed the Rule column from the list.
* Setting the product picker cap to 5 turned the picker into 5 rows plus
  "42 more products not shown".
* All keys were removed again afterwards, so the host is back on the shipped defaults.
