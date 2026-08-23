using Dynamicweb.Auditing;
using Dynamicweb.Data;

namespace Truvio.Commerce.PowerTools.Core.Operations.Dw;

/// <summary>
/// "Who changed what recently", assembled from the three places a DW10 install already keeps
/// that information — nothing is inferred and nothing is invented.
/// <list type="number">
/// <item><b>CommandLog</b> — every admin API command DW executed, with the user name and the
/// request URL. This is the only source with reliable attribution, and it is present on every
/// DW10 install.</item>
/// <item><b>Audit</b> — DW's audit trail, read through
/// <c>Dynamicweb.Auditing.AuditService.GetByQuery(AuditQuery)</c>. It is empty unless
/// <c>/Globalsettings/Settings/Auditing/EnableAuditing</c> is on, which is why it supplements
/// rather than replaces the command log.</item>
/// <item><b>File timestamps</b> — scheduled-task and data-integration definitions and
/// GlobalSettings.config are files; their last-write times say when the configuration changed,
/// but the file system keeps no author, so those rows say "unknown".</item>
/// </list>
/// </summary>
internal static class DwChangeReader
{
    public const string CommandLogSource = "Admin commands";
    public const string AuditSource = "Audit trail";
    public const string FileSource = "Configuration files";

    public static IReadOnlyList<ChangeSpec> GetRecentChanges(int days, int max)
    {
        var since = DateTime.Now.AddDays(-Math.Max(days, 1));
        var changes = new List<ChangeSpec>();

        changes.AddRange(ReadCommandLog(since, max));
        changes.AddRange(ReadAudit(since, max));
        changes.AddRange(ReadFileChanges(since));

        return changes.OrderByDescending(c => c.When).Take(max).ToList();
    }

    private static List<ChangeSpec> ReadCommandLog(DateTime since, int max)
    {
        var changes = new List<ChangeSpec>();
        try
        {
            var sql = CommandBuilder.Create(
                """
                SELECT TOP ({0}) CommandLogTimestamp, CommandLogCommandType, CommandLogAccessUserName, CommandLogRequestUrl
                FROM CommandLog
                WHERE CommandLogTimestamp >= {1}
                ORDER BY CommandLogId DESC
                """,
                max,
                since);

            using var reader = Database.CreateDataReader(sql);
            while (reader.Read())
            {
                var commandType = Text(reader["CommandLogCommandType"]);
                var user = Text(reader["CommandLogAccessUserName"]);
                changes.Add(new ChangeSpec(
                    When: reader["CommandLogTimestamp"] is DateTime when ? when : DateTime.MinValue,
                    Who: string.IsNullOrWhiteSpace(user) ? "unknown" : user,
                    What: DescribeCommand(commandType),
                    Where: CommandLogSource));
            }
        }
        catch
        {
            // No command log on this install; the other sources still contribute.
        }

        return changes;
    }

    /// <summary>
    /// "Dynamicweb.Products.UI.Commands.ProductSaveCommand" → "Product save".
    /// The command class name is the only description DW stores, so it is unpacked rather than
    /// guessed at: strip namespace and the "Command" suffix, then split on word boundaries.
    /// </summary>
    internal static string DescribeCommand(string commandType)
    {
        var name = OpsFormat.ShortTypeName(commandType);
        if (name.EndsWith("Command", StringComparison.Ordinal) && name.Length > "Command".Length)
            name = name[..^"Command".Length];

        if (name.Length == 0)
            return "Unknown command";

        var words = new List<string>();
        var start = 0;
        for (var i = 1; i < name.Length; i++)
        {
            if (!char.IsUpper(name[i]))
                continue;
            words.Add(name[start..i]);
            start = i;
        }
        words.Add(name[start..]);

        return string.Join(' ', words.Select((w, i) => i == 0 ? w : w.ToLowerInvariant()));
    }

    private static List<ChangeSpec> ReadAudit(DateTime since, int max)
    {
        try
        {
            var query = new AuditQuery
            {
                StartTime = since,
                EndTime = DateTime.Now,
                TopNResults = max
            };

            var events = new AuditService().GetByQuery(query).ToList();
            if (events.Count == 0)
                return [];

            var names = UserNames(events.Select(e => e.UserId).Distinct());

            return events.Select(e => new ChangeSpec(
                When: e.Timestamp,
                Who: names.TryGetValue(e.UserId, out var name) && !string.IsNullOrWhiteSpace(name) ? name : "unknown",
                What: string.IsNullOrWhiteSpace(e.Action) ? e.Type : $"{e.Type} {e.Action.ToLowerInvariant()} ({e.Id})",
                Where: AuditSource)).ToList();
        }
        catch
        {
            return [];
        }
    }

    private static Dictionary<int, string> UserNames(IEnumerable<int> userIds)
    {
        var ids = userIds.Where(id => id > 0).ToList();
        var names = new Dictionary<int, string>();
        if (ids.Count == 0)
            return names;

        try
        {
            var sql = CommandBuilder.Create(
                "SELECT AccessUserId, AccessUserName, AccessUserUserName FROM AccessUser WHERE AccessUserId IN ({0})",
                ids);

            using var reader = Database.CreateDataReader(sql);
            while (reader.Read())
            {
                var display = Text(reader["AccessUserName"]);
                if (string.IsNullOrWhiteSpace(display))
                    display = Text(reader["AccessUserUserName"]);
                names[Convert.ToInt32(reader["AccessUserId"])] = display;
            }
        }
        catch
        {
            // Attribution stays "unknown" rather than being invented.
        }

        return names;
    }

    /// <summary>
    /// Definition files carry no author, so these rows are explicitly attributed to "unknown".
    /// They still answer "what moved recently" when the command log has been trimmed.
    /// </summary>
    private static List<ChangeSpec> ReadFileChanges(DateTime since)
    {
        var changes = new List<ChangeSpec>();

        AddFile(DwPaths.GlobalSettingsRelative, "Global settings changed");

        AddFolder(DwPaths.ActivityFolderRelative, "*.xml", "Integration activity saved", recurse: true);
        AddFolder(DwPaths.TaskXmlFolderRelative, "*.xml", "Scheduled task definition saved", recurse: false);

        return changes;

        void AddFile(string relativePath, string what)
        {
            var physical = DwPaths.Map(relativePath);
            var modified = DwActivityReader.SafeWriteTime(physical);
            if (modified is { } when && when >= since)
                changes.Add(new ChangeSpec(when, "unknown", what, FileSource));
        }

        void AddFolder(string relativePath, string pattern, string what, bool recurse)
        {
            var physical = DwPaths.Map(relativePath);
            if (string.IsNullOrEmpty(physical) || !Directory.Exists(physical))
                return;

            try
            {
                var option = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                foreach (var file in new DirectoryInfo(physical).GetFiles(pattern, option))
                {
                    if (file.LastWriteTime >= since)
                        changes.Add(new ChangeSpec(file.LastWriteTime, "unknown", $"{what}: {Path.GetFileNameWithoutExtension(file.Name)}", FileSource));
                }
            }
            catch
            {
                // An unreadable folder contributes nothing.
            }
        }
    }

    private static string Text(object? value) => value is null || value is DBNull ? string.Empty : value.ToString() ?? string.Empty;
}
