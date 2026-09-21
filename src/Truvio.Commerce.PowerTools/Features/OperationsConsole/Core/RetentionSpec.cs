namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Core;

/// <summary>
/// DW's log-retention configuration, read from GlobalSettings. When purging is off nothing
/// ever rotates — the single most common cause of runaway log folders and log tables.
/// </summary>
/// <param name="PurgeEnabled">/Globalsettings/Settings/Logging/FilesRetentionSettings/PurgeEnabled.</param>
/// <param name="FileLocations">Configured file locations, DW default is /System/Log and /System/Diagnostics.</param>
/// <param name="DbTables">Tables covered by the database retention settings.</param>
public sealed record RetentionSpec(
    bool PurgeEnabled,
    IReadOnlyList<string> FileLocations,
    IReadOnlyList<string> DbTables)
{
    public static RetentionSpec Unknown => new(false, [], []);

    public bool CoversTable(string tableName) =>
        DbTables.Any(t => string.Equals(t, tableName, StringComparison.OrdinalIgnoreCase));
}
