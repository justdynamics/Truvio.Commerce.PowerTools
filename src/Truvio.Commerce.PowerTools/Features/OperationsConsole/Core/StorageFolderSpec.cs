namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Core;

/// <summary>A log folder (or other file store) with its aggregate size and age span.</summary>
/// <param name="Name">Display name, e.g. "System/Log/ScheduledTasks".</param>
/// <param name="RelativePath">DW-relative path, e.g. "/Files/System/Log/ScheduledTasks".</param>
/// <param name="Bytes">Total bytes of all files directly in the folder.</param>
/// <param name="FileCount">Number of files directly in the folder.</param>
/// <param name="Oldest">Oldest file's last-write time.</param>
/// <param name="Newest">Newest file's last-write time.</param>
public sealed record StorageFolderSpec(
    string Name,
    string RelativePath,
    long Bytes,
    int FileCount,
    DateTime? Oldest,
    DateTime? Newest)
{
    /// <summary>Days between the oldest and newest file, i.e. how much history the folder keeps.</summary>
    public double SpanDays => Oldest is { } o && Newest is { } n && n > o ? (n - o).TotalDays : 0;
}
