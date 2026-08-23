using System.Xml.Linq;
using DwTask = Dynamicweb.Scheduling.Task;

namespace Truvio.Commerce.PowerTools.Core.Operations.Dw;

/// <summary>
/// Reads data-integration activities straight off disk.
/// <para>
/// An activity is a job XML file under <c>/Files/{filesFolder}/Integration/jobs</c>, optionally
/// inside a group folder. Its identifier — the value a scheduled task stores to reference it —
/// is <c>group\name</c>, or just <c>name</c> at the root (<c>Job.GetJobIdentifier</c>).
/// </para>
/// <para>
/// The definitions are parsed rather than loaded through <c>Job.GetJobInformation</c>/the
/// <c>Job(path, logFile)</c> constructor on purpose: constructing a Job instantiates the source
/// and destination provider add-ins, and <c>Job.GetJobFiles</c> creates the jobs folder when it
/// is missing. This tool must not write anything, and must not execute provider code.
/// </para>
/// <para>
/// Run state comes from the marker files DW writes next to the run logs under
/// <c>/Files/System/Log/DataIntegration[/group]</c>: <c>{name}_lastrun.log</c> holds the start
/// timestamp and <c>{name}_lastrunresult.log</c> holds a <c>JobResult</c> name
/// (Unknown/Completed/Failed/CompletedWithError) — see <c>Job.LastRun</c> / <c>Job.LastRunResult</c>.
/// </para>
/// </summary>
internal static class DwActivityReader
{
    public static IReadOnlyList<ActivitySpec> GetActivities()
    {
        var root = DwPaths.Map(DwPaths.ActivityFolderRelative);
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            return [];

        var activities = new List<ActivitySpec>();
        try
        {
            foreach (var file in Directory.EnumerateFiles(root, "*.xml", SearchOption.TopDirectoryOnly))
                activities.Add(ReadActivity(file, group: string.Empty));

            foreach (var groupDir in Directory.EnumerateDirectories(root))
            {
                var group = Path.GetFileName(groupDir);
                foreach (var file in Directory.EnumerateFiles(groupDir, "*.xml", SearchOption.TopDirectoryOnly))
                    activities.Add(ReadActivity(file, group));
            }
        }
        catch
        {
            // A partially readable jobs folder still yields whatever was read before the failure.
        }

        return activities.OrderBy(a => a.Group, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
                         .ToList();
    }

    private static ActivitySpec ReadActivity(string filePath, string group)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);
        var id = string.IsNullOrEmpty(group) ? name : $"{group}\\{name}";

        var description = string.Empty;
        var source = string.Empty;
        var destination = string.Empty;
        var tables = 0;
        var mappings = 0;
        var columnMappings = 0;

        try
        {
            var job = LoadJobXml(filePath)?.Root;
            if (job is not null)
            {
                name = Element(job, "Name") is { Length: > 0 } n ? n : name;
                description = Element(job, "Description") ?? string.Empty;

                var sourceNode = Child(job, "source");
                var destinationNode = Child(job, "destination");
                source = (string?)sourceNode?.Attribute("type") ?? string.Empty;
                destination = (string?)destinationNode?.Attribute("type") ?? string.Empty;

                tables = sourceNode?.Descendants()
                    .Count(e => string.Equals(e.Name.LocalName, "table", StringComparison.OrdinalIgnoreCase)) ?? 0;

                var mappingsNode = Child(job, "mappings");
                mappings = mappingsNode?.Elements()
                    .Count(e => string.Equals(e.Name.LocalName, "Mapping", StringComparison.OrdinalIgnoreCase)) ?? 0;
                columnMappings = mappingsNode?.Descendants()
                    .Count(e => string.Equals(e.Name.LocalName, "ColumnMapping", StringComparison.OrdinalIgnoreCase)) ?? 0;
            }
        }
        catch
        {
            description = "The job file could not be parsed.";
        }

        var (lastRun, lastResult, duration) = ReadRunState(name, group);

        return new ActivitySpec(
            Id: id,
            Name: name,
            Group: group,
            Description: description,
            SourceProvider: source,
            DestinationProvider: destination,
            TableCount: tables,
            MappingCount: mappings,
            ColumnMappingCount: columnMappings,
            LastRun: lastRun,
            LastResult: lastResult,
            LastDuration: duration,
            ModifiedAt: SafeWriteTime(filePath));
    }

    /// <summary>
    /// Job files are read the way DW reads them — through a <see cref="StreamReader"/> with
    /// BOM detection (<c>Job.GetJobInformation</c> does exactly this). It matters: DW writes
    /// every job file with an <c>encoding="utf-16"</c> declaration but saves some of them as
    /// UTF-8 with no BOM, so loading by path (which honours the declaration) throws on those
    /// files. Handing the parser a TextReader makes it use the reader's encoding and ignore the
    /// declaration, which is why DW itself never trips over its own files.
    /// </summary>
    private static XDocument? LoadJobXml(string filePath)
    {
        using var reader = new StreamReader(filePath, detectEncodingFromByteOrderMarks: true);
        return XDocument.Load(reader, LoadOptions.None);
    }

    private static (DateTime? LastRun, string LastResult, TimeSpan? Duration) ReadRunState(string name, string group)
    {
        var folder = LogFolderFor(group);
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            return (null, "Unknown", null);

        var safeName = SafeName(name);
        var lastRunFile = Path.Combine(folder, $"{safeName}_lastrun.log");
        var resultFile = Path.Combine(folder, $"{safeName}_lastrunresult.log");

        DateTime? lastRun = null;
        if (File.Exists(lastRunFile) && DateTime.TryParse(ReadAllTextSafe(lastRunFile), out var parsed))
            lastRun = parsed;

        var result = File.Exists(resultFile) ? ReadAllTextSafe(resultFile).Trim() : string.Empty;
        if (string.IsNullOrEmpty(result))
            result = "Unknown";

        TimeSpan? duration = null;
        if (lastRun is { } start)
        {
            var end = File.Exists(resultFile) ? SafeWriteTime(resultFile) : null;
            if (end is { } finish && finish > start)
                duration = finish - start;
        }

        if (lastRun is null)
        {
            var newest = RunLogs(name, group).FirstOrDefault();
            if (newest is not null)
                lastRun = newest.LastWriteTime;
        }

        return (lastRun, result, duration);
    }

    /// <summary>The newest run-log lines for an activity, oldest line first.</summary>
    public static IReadOnlyList<string> GetLogTail(string activityId, int maxLines)
    {
        var id = ActivityLinks.Normalise(activityId);
        var separator = id.LastIndexOf('\\');
        var group = separator > 0 ? id[..separator] : string.Empty;
        var name = separator > 0 ? id[(separator + 1)..] : id;

        var newest = RunLogs(name, group).FirstOrDefault();
        if (newest is null)
            return [];

        try
        {
            var lines = File.ReadLines(newest.FullName).ToList();
            return lines.Count <= maxLines ? lines : lines.Skip(lines.Count - maxLines).ToList();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Run logs are named <c>{safeName}{yyyyMMdd-HHmmss…}.log</c> in the activity's log folder,
    /// alongside the <c>_lastrun</c> markers (which the prefix match must not pick up).
    /// </summary>
    private static List<FileInfo> RunLogs(string name, string group)
    {
        var folder = LogFolderFor(group);
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            return [];

        var safeName = SafeName(name);
        try
        {
            return new DirectoryInfo(folder)
                .GetFiles($"{safeName}*.log")
                .Where(f => !f.Name.EndsWith("_lastrun.log", StringComparison.OrdinalIgnoreCase)
                         && !f.Name.EndsWith("_lastrunresult.log", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(f => f.LastWriteTime)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static string LogFolderFor(string group)
    {
        var root = DwPaths.Map(DwPaths.DataIntegrationLogRelative);
        if (string.IsNullOrEmpty(root))
            return string.Empty;
        return string.IsNullOrEmpty(group) ? root : Path.Combine(root, group);
    }

    /// <summary>DW strips invalid path characters via <c>Task.MakeSafeFileName</c>; match it exactly.</summary>
    private static string SafeName(string name)
    {
        try
        {
            return DwTask.MakeSafeFileName(name);
        }
        catch
        {
            return name;
        }
    }

    private static string ReadAllTextSafe(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch
        {
            return string.Empty;
        }
    }

    internal static DateTime? SafeWriteTime(string path)
    {
        try
        {
            return File.Exists(path) ? File.GetLastWriteTime(path) : null;
        }
        catch
        {
            return null;
        }
    }

    private static XElement? Child(XElement parent, string localName) =>
        parent.Elements().FirstOrDefault(e => string.Equals(e.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase));

    private static string? Element(XElement parent, string localName) => Child(parent, localName)?.Value;
}
