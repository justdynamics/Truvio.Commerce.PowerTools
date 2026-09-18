namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Core;

/// <summary>A data-integration activity (a job XML file under the integration jobs folder).</summary>
/// <param name="Id">Activity identifier — <c>group\name</c>, or just <c>name</c> at the root.</param>
/// <param name="Name">Activity name.</param>
/// <param name="Group">Group folder, empty for root-level activities.</param>
/// <param name="Description">Description stored in the job XML.</param>
/// <param name="SourceProvider">Source provider type name.</param>
/// <param name="DestinationProvider">Destination provider type name.</param>
/// <param name="TableCount">Number of source tables in the job schema.</param>
/// <param name="MappingCount">Number of table mappings.</param>
/// <param name="ColumnMappingCount">Number of column mappings across all table mappings.</param>
/// <param name="LastRun">Last run time from DW's <c>_lastrun.log</c> marker (or the newest run log).</param>
/// <param name="LastResult">DW's <c>JobResult</c> name from the <c>_lastrunresult.log</c> marker.</param>
/// <param name="LastDuration">Elapsed time of the last run, when derivable.</param>
/// <param name="ModifiedAt">Last write time of the job XML file.</param>
public sealed record ActivitySpec(
    string Id,
    string Name,
    string Group,
    string Description,
    string SourceProvider,
    string DestinationProvider,
    int TableCount,
    int MappingCount,
    int ColumnMappingCount,
    DateTime? LastRun,
    string LastResult,
    TimeSpan? LastDuration,
    DateTime? ModifiedAt)
{
    public string SourceShortName => OpsFormat.ShortTypeName(SourceProvider);

    public string DestinationShortName => OpsFormat.ShortTypeName(DestinationProvider);
}
