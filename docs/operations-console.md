# Operations Console

**PowerTools ▸ Operations** — read-only. One place that answers *is this install healthy, what ran,
what failed, what is growing, and who changed what?*

Permission: `truvio-powertools-operations` (Read). The section and every node are hidden without it.

Community requests this merges (from the Dynamicweb feature-request tracker): scheduled-task run
history and trends, "who ran this task", 20 GB of log files nobody trims, configuration version
history, broken task→activity links, a `CommandLog` grown to 14.8 GB of an 18 GB database, an event
viewer capped at 500 rows, and truncated exception text.

## Screens

| Node | Screen | What it answers |
|---|---|---|
| Health | `OperationsHealthScreen` | Everything at once: verdict, task/activity/storage counts, all findings |
| Scheduled tasks | `ScheduledTaskListScreen` → `ScheduledTaskDetailScreen` | What runs, what failed, what is stale; per task: definition, saved parameters, last 20 runs |
| Integration activities | `IntegrationActivityListScreen` → `IntegrationActivityDetailScreen` | Every activity, its providers, its last run, and which task schedules it |
| Logs & storage | `LogsStorageScreen` | Log folders by size, the 15 largest tables, the retention settings, storage findings |
| Recent changes | `RecentChangeListScreen` | Who changed what, over a selectable window |

Report screens use `OverviewScreenBase` + `HtmlBlock` tables rather than the list grid: the grid gives
every column equal width and clips overflow, and explanations are long text. List screens keep to six
short columns for the same reason.

## Rules

Pure, in `Core/Operations/Rules/`, unit-tested against hand-built snapshots. Ids are stable.

| Id | Rule | Fires when | Severity |
|---|---|---|---|
| OPS-W1 | `FailingTaskRule` | Enabled task, last run recorded as failed (`TaskLastResult = 0`) or with stored exception text | Critical |
| OPS-W2 | `StaleTaskRule` | Enabled repeating task whose last run is older than **2×** its own interval | Warning |
| OPS-W3 | `StaleTaskRule` | Enabled task that has never run | Warning |
| OPS-W4 | `BrokenActivityLinkRule` | Task names an activity identifier no job file backs | Critical (Warning if no activities were readable at all) |
| OPS-W5 | `BrokenActivityLinkRule` | Activity that no *enabled* task runs; above 5 such activities they collapse into one finding | Info |
| OPS-W6 | `LogGrowthRule` | Log folder ≥ 500 MB (Warning) / ≥ 2 GB (Critical) | Warning / Critical |
| OPS-W7 | `LogGrowthRule` | Folder keeps > 30 days of files **and** log purging is off | Warning |
| OPS-W8 | `TableBloatRule` | One table ≥ 25 % of the database and ≥ 10 MB; Critical from 1 GB | Warning / Critical |
| OPS-W9 | `TableBloatRule` | Known append-only table ≥ 100 000 rows that database retention does not cover | Warning |
| OPS-E1 | `OperationsHealthEngine` | A rule threw — one broken reader must not hide the other findings | Info |

Why 2× the interval for staleness: DW's own `Task.LastRunState` has a `HasNotRunAsItShould` value,
but it goes true the moment `UpcomingRuntime` slips one second into the past, so it is unusable as an
alert. The interval-based window fires only on a task that has genuinely stopped.

Why the floors on OPS-W8/W9: a share alone makes any small development database look like it has one
dominant table, and the share is measured against the **whole** database (`GetDatabaseBytes()`), not
against the top-15 list the screen shows.

## DW facts verified (decompiled 10.8.4 unless noted)

**Scheduled tasks**

- `Dynamicweb.Scheduling.TaskService` is public with a public constructor; `GetTasks()` / `GetTaskById(int)`
  read from a process-wide cache. Used for every task read.
- `Dynamicweb.Scheduling.Task` exposes `ID`, `Name`, `Enabled`, `Minute` (the repeat interval),
  `Schedule` (DW's own human-readable summary), `LastRun` (null when never run — DW stores "never" as
  the `Consts.MinDate` sentinel `2000-01-01`), `UpcomingRuntime`, `LastResult`, `LastException`,
  `AddInTypeName`, `AddInSettings`, `Comment`, and the public helper `Task.MakeSafeFileName(string)`.
- `TaskService.GetLastExecutionsLogs(int taskId, int lastExecutionsAmount)` reconstructs runs by
  scanning `/Files/System/Log/ScheduledTasks/*.log` in reverse for the task's
  `Task 'name' with Id 'n'` marker (`Task.IdAndName`). It carries **no attribution** — used only as a
  fallback.
- The **`ScheduledTaskExecution`** table (verified on a 10.27.9 install) is the richer source:
  `…TaskId`, `…ScheduleTime`, `…StartTime`, `…EndTime`, `…UserId`, `…Result`, `…Output`. `UserId` is
  `NULL` for unattended runs, which is where the "who ran this" column comes from — it reads
  *Scheduler* when null and the `AccessUser` name otherwise. Not present on every DW10 version, so the
  read is probed and falls back.
- Add-in settings are DW's standard parameter XML
  (`<Parameters><Parameter name="x" value="y" /></Parameters>`); longer values are written as element
  content instead of a `value` attribute, so both are read.

**Data-integration activities**

- An activity is a job XML file under `Job.JobFolder` =
  `MapPath("/Files/{filesFolder}/Integration/jobs/")`. The files-folder name comes from
  `/Globalsettings/System/Filesystem/FilesFolderName`, defaulting to `Files` — the same lookup
  `Task.GetFilesFolderName()` performs.
- Its identifier — what a scheduled task stores to reference it — is `group\name`, or just `name` at
  the root (`Job.GetJobIdentifier(string folder, string jobName)`).
- The link is one-way and unvalidated: `Dynamicweb.DataIntegration.Integration.JobScheduledTaskAddIn`
  (add-in name `RunDataIntegrationJobAddIn`) keeps the identifier in an add-in parameter named
  **`Activity`** and resolves it lazily inside `Run()` via `JobQueue.RunJob(Activity, Task)`. Nothing
  checks it at save time, which is exactly the "broken task→activity link" breakage OPS-W4 reports.
- Run state comes from the marker files DW writes beside the run logs in
  `/Files/System/Log/DataIntegration[/{group}]`: `{safeName}_lastrun.log` holds the start timestamp and
  `{safeName}_lastrunresult.log` holds a `JobResult` name — `Unknown` / `Completed` / `Failed` /
  `CompletedWithError` (`Job.LastRun`, `Job.LastRunResult`). `safeName` is `Task.MakeSafeFileName(Name)`.
  Run logs are `{safeName}{yyyyMMdd-HHmmss…}.log` in the same folder.
- **The job files are parsed, not loaded through the DW API**, on purpose:
  - `Job.GetJobFiles()` calls `Directory.CreateDirectory(JobFolder)` when the folder is missing — a write.
  - The `Job(string path, string logFile)` constructor instantiates the source and destination provider
    add-ins, i.e. it executes third-party code just to render a list.
  - Consequently the tool needs no `Dynamicweb.DataIntegration` package reference at all.
- **Encoding trap (verified on a real install):** DW writes every job file with an
  `encoding="utf-16"` declaration but saves some of them as UTF-8 with no BOM. `XDocument.Load(path)`
  honours the declaration and throws on those files. DW itself never trips over this because
  `Job.GetJobInformation` reads through `new StreamReader(path, detectEncodingFromByteOrderMarks: true)`
  and hands the parser a `TextReader`, which makes it use the reader's encoding and ignore the
  declaration. `DwActivityReader.LoadJobXml` mirrors that exactly. Before the fix, 6 of 13 activities on
  the verification host showed no provider at all.

**Logs, storage and retention**

- File logs live under `/Files/System/Log` (`LogPathPrefix` in `Dynamicweb.Logging`); the second
  default retention location is `/Files/System/Diagnostics`.
- Retention is owned by `Dynamicweb.Logging.ScheduledTaskAddIns.LogsCleanupScheduledTaskAddIn`
  ("Cleanup logs"), and every part of it is gated on one setting:
  - `/Globalsettings/Settings/Logging/FilesRetentionSettings/PurgeEnabled` — **off, nothing is ever
    trimmed**, neither files nor tables.
  - `…/FilesRetentionSettings/LogLocations` — pipe-separated, defaulting to
    `/System/Log|/System/Diagnostics`; plus `IncludeSubFolders{location}` and `Retention{location}`
    (days, default 30).
  - `…/DBRetentionSettings/TableNames` — pipe-separated; plus `ColumnName{table}` and
    `Retention{table}` (days, default 30). A log table not in this list is never trimmed however large
    it grows.
- Table sizes come from `sys.dm_db_partition_stats` through `Dynamicweb.Data.Database.CreateDataReader`,
  read-only: reserved pages summed over **all** indexes for the footprint, `row_count` taken only from
  `index_id IN (0,1)` so non-clustered copies are not counted twice. It needs VIEW DATABASE STATE; the
  screen says so explicitly when the read fails rather than showing zeroes.

**Recent changes** — three sources, nothing inferred:

1. `CommandLog` (`CommandLogTimestamp`, `CommandLogCommandType`, `CommandLogAccessUserName`,
   `CommandLogRequestUrl`) — every admin API command, with real attribution. Present on every DW10
   install. The command class name is the only description DW stores, so it is unpacked rather than
   guessed at: `…Commands.ProductSaveCommand` → "Product save".
2. `Dynamicweb.Auditing.AuditService.GetByQuery(AuditQuery)` — public, instantiable at 10.8.4, but the
   audit trail is empty unless `/Globalsettings/Settings/Auditing/EnableAuditing` is on (it was off on
   the verification host, `Audit` had 0 rows), so it supplements rather than replaces the command log.
3. File timestamps: the job XML files, the XML scheduled-task folder (`Task.XmlFolder`), and
   `GlobalSettings.config`. The file system keeps no author, so those rows say **"unknown"** — never a
   guess.

**CoreUI**

- `BadgeType.Muted` renders as an all-but-invisible badge in the DW10 grid and info bar. `Secondary` is
  the quietest style that still reads; the "Disabled" task badge and the "Unknown" activity result both
  had to move off `Muted` after seeing them on screen.
- Every public property of a query class is serialised into the screen URL, so `RecentChangeListQuery.Days`
  round-trips as a shareable URL and the day presets are plain `NavigateScreenAction`s.
- Icons used all exist at the 10.8.4 floor: `Heartbeat`, `Schedule`, `ExchangeAlt`, `Database`, `History`.

## Verified on

Dynamicweb **10.27.9** (host `cabp`, port 55620): 3 scheduled tasks, 13 data-integration activities,
4 543 `ScheduledTaskExecution` rows, 2 362 `CommandLog` rows, 1.2 MB of log files, 38.7 MB database.
Floor build (`-p:DynamicwebVersion=10.8.4`) green; no new package reference.
