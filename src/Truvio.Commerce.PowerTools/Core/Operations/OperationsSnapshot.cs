using Truvio.Commerce.PowerTools.Core.Diagnostics;

namespace Truvio.Commerce.PowerTools.Core.Operations;

/// <summary>
/// One consistent read of the install that every rule evaluates against. Taking a snapshot
/// first (instead of letting each rule query DW) keeps the rules pure and makes the health
/// counts on the Health screen agree with the numbers on the detail screens.
/// </summary>
/// <param name="Tables">The largest tables only — never assume this is the whole database.</param>
/// <param name="DatabaseBytes">
/// Reserved bytes of the whole database. Passed in separately because <paramref name="Tables"/>
/// is a top-N list: computing a table's share against the listed tables alone would make every
/// install look like it had one dominant table.
/// </param>
public sealed record OperationsSnapshot(
    IReadOnlyList<TaskSpec> Tasks,
    IReadOnlyList<ActivitySpec> Activities,
    IReadOnlyList<StorageFolderSpec> LogFolders,
    IReadOnlyList<TableSizeSpec> Tables,
    RetentionSpec Retention,
    DateTime Now,
    long DatabaseBytes = 0)
{
    public static OperationsSnapshot Empty => new([], [], [], [], RetentionSpec.Unknown, DateTime.Now);

    /// <summary>Whole-database size, falling back to the sum of the listed tables.</summary>
    public long TotalTableBytes => DatabaseBytes > 0 ? DatabaseBytes : Tables.Sum(t => t.Bytes);

    /// <summary>Total bytes across every log folder read.</summary>
    public long TotalLogBytes => LogFolders.Sum(f => f.Bytes);
}

/// <summary>A rule over one <see cref="OperationsSnapshot"/>. Rule ids stay stable: OPS-W1..</summary>
public interface IOperationsRule
{
    IEnumerable<Finding> Evaluate(OperationsSnapshot snapshot);
}
