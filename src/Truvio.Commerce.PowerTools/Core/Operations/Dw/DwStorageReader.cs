using Dynamicweb.Configuration;
using Dynamicweb.Core;
using Dynamicweb.Data;

namespace Truvio.Commerce.PowerTools.Core.Operations.Dw;

/// <summary>
/// Reads what is growing: log folders on disk and table sizes in the database, plus the
/// retention configuration that decides whether either is ever trimmed.
/// </summary>
internal static class DwStorageReader
{
    /// <summary>How deep below a log root to report folders separately.</summary>
    private const int MaxDepth = 2;

    public static IReadOnlyList<StorageFolderSpec> GetLogFolders()
    {
        var folders = new List<StorageFolderSpec>();
        foreach (var root in new[] { DwPaths.LogRootRelative, DwPaths.DiagnosticsRootRelative })
        {
            var physical = DwPaths.Map(root);
            if (string.IsNullOrEmpty(physical) || !Directory.Exists(physical))
                continue;

            Collect(new DirectoryInfo(physical), root, folders, depth: 0);
        }

        return folders.Where(f => f.FileCount > 0)
                      .OrderByDescending(f => f.Bytes)
                      .ToList();
    }

    private static void Collect(DirectoryInfo directory, string relativePath, List<StorageFolderSpec> into, int depth)
    {
        FileInfo[] files;
        DirectoryInfo[] children;
        try
        {
            files = directory.GetFiles();
            children = directory.GetDirectories();
        }
        catch
        {
            return;
        }

        var bytes = files.Sum(f => SafeLength(f));
        var oldest = files.Length == 0 ? (DateTime?)null : files.Min(f => f.LastWriteTime);
        var newest = files.Length == 0 ? (DateTime?)null : files.Max(f => f.LastWriteTime);

        into.Add(new StorageFolderSpec(
            Name: relativePath.TrimStart('/').Replace("Files/", string.Empty, StringComparison.OrdinalIgnoreCase),
            RelativePath: relativePath,
            Bytes: bytes,
            FileCount: files.Length,
            Oldest: oldest,
            Newest: newest));

        if (depth >= MaxDepth)
            return;

        foreach (var child in children)
            Collect(child, $"{relativePath}/{child.Name}", into, depth + 1);
    }

    private static long SafeLength(FileInfo file)
    {
        try
        {
            return file.Length;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Table sizes from <c>sys.dm_db_partition_stats</c>: reserved pages summed over every index
    /// (the real footprint), rows taken from the heap/clustered index only (index_id 0 or 1) so
    /// non-clustered copies are not counted twice. Read-only, and it needs no more than the
    /// VIEW DATABASE STATE the DW connection normally has.
    /// </summary>
    public static IReadOnlyList<TableSizeSpec> GetTableSizes(int top)
    {
        var tables = new List<TableSizeSpec>();
        try
        {
            var sql = CommandBuilder.Create(
                """
                SELECT TOP ({0}) t.name AS TableName,
                       SUM(CASE WHEN p.index_id IN (0,1) THEN p.row_count ELSE 0 END) AS RowCountValue,
                       SUM(p.reserved_page_count) * 8192 AS ReservedBytes
                FROM sys.dm_db_partition_stats p
                INNER JOIN sys.tables t ON t.object_id = p.object_id
                GROUP BY t.name
                ORDER BY SUM(p.reserved_page_count) DESC
                """,
                top);

            using var reader = Database.CreateDataReader(sql);
            while (reader.Read())
            {
                tables.Add(new TableSizeSpec(
                    Convert.ToString(reader["TableName"]) ?? string.Empty,
                    Convert.ToInt64(reader["RowCountValue"]),
                    Convert.ToInt64(reader["ReservedBytes"])));
            }
        }
        catch
        {
            return [];
        }

        return tables;
    }

    /// <summary>Total reserved bytes of every user table, so shares are computed against the whole DB.</summary>
    public static long GetDatabaseBytes()
    {
        try
        {
            var value = Database.ExecuteScalar(
                "SELECT SUM(CAST(p.reserved_page_count AS bigint)) * 8192 FROM sys.dm_db_partition_stats p " +
                "INNER JOIN sys.tables t ON t.object_id = p.object_id");
            return value is null or DBNull ? 0 : Convert.ToInt64(value);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// DW's log retention lives in GlobalSettings and is driven by the "Cleanup logs" scheduled
    /// task (<c>Dynamicweb.Logging.ScheduledTaskAddIns.LogsCleanupScheduledTaskAddIn</c>). When
    /// <c>PurgeEnabled</c> is off — the default — the task does nothing at all, which is the
    /// root cause behind both runaway log folders and runaway log tables.
    /// </summary>
    public static RetentionSpec GetRetention()
    {
        try
        {
            var config = SystemConfiguration.Instance;
            var purge = Converter.ToBoolean(config.GetValue("/Globalsettings/Settings/Logging/FilesRetentionSettings/PurgeEnabled"));

            var locations = config.GetValue("/Globalsettings/Settings/Logging/FilesRetentionSettings/LogLocations");
            var fileLocations = string.IsNullOrWhiteSpace(locations)
                ? new[] { "/System/Log", "/System/Diagnostics" }
                : locations.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var tableNames = Converter.ToString(config.GetValue("/Globalsettings/Settings/Logging/DBRetentionSettings/TableNames"));
            var dbTables = string.IsNullOrWhiteSpace(tableNames)
                ? Array.Empty<string>()
                : tableNames.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return new RetentionSpec(purge, fileLocations, dbTables);
        }
        catch
        {
            return RetentionSpec.Unknown;
        }
    }

    /// <summary>Retention days configured for one file location or table; DW's own default is 30.</summary>
    public static int GetRetentionDays(string settingsGroup, string key)
    {
        try
        {
            var value = SystemConfiguration.Instance.GetValue($"/Globalsettings/Settings/Logging/{settingsGroup}/Retention{key}");
            var days = string.IsNullOrWhiteSpace(value) ? 30 : Converter.ToInt32(value);
            return days < 0 ? 30 : days;
        }
        catch
        {
            return 30;
        }
    }
}
