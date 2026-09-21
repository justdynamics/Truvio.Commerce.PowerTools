using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.OperationsConsole.OperationsTestData;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Core;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Core.Rules;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Tests.Features.OperationsConsole;

public class TableBloatRuleTests
{
    private const long Mb = 1024 * 1024;

    private static IReadOnlyList<Finding> Run(OperationsSnapshot snapshot) =>
        new TableBloatRule().Evaluate(snapshot).ToList();

    [Fact]
    public void TableOverAQuarterOfTheDatabase_IsReported()
    {
        var snapshot = Snapshot(
            tables: [Table("CommandLog", rows: 10, bytes: 60 * Mb), Table("Page", rows: 10, bytes: 40 * Mb)],
            databaseBytes: 100 * Mb);

        var finding = Run(snapshot).Single(f => f.RuleId == TableBloatRule.ShareId && f.EntityKey == "CommandLog");
        Assert.Equal(FindingSeverity.Warning, finding.Severity);
        Assert.Contains("60", finding.Title);
    }

    [Fact]
    public void ShareIsMeasuredAgainstTheWholeDatabase_NotTheListedTables()
    {
        // 30 MB of a 1 GB database is 3%, even though it is the only listed table.
        var snapshot = Snapshot(tables: [Table("CommandLog", bytes: 30 * Mb)], databaseBytes: 1024 * Mb);

        Assert.DoesNotContain(Run(snapshot), f => f.RuleId == TableBloatRule.ShareId);
    }

    [Fact]
    public void HugeTable_IsCritical()
    {
        var snapshot = Snapshot(tables: [Table("CommandLog", bytes: 2048 * Mb)], databaseBytes: 4096 * Mb);

        var finding = Run(snapshot).Single(f => f.RuleId == TableBloatRule.ShareId);
        Assert.Equal(FindingSeverity.Critical, finding.Severity);
    }

    [Fact]
    public void TinyDatabase_ProducesNoShareNoise()
    {
        var snapshot = Snapshot(tables: [Table("CommandLog", bytes: 2 * Mb)], databaseBytes: 4 * Mb);

        Assert.DoesNotContain(Run(snapshot), f => f.RuleId == TableBloatRule.ShareId);
    }

    [Fact]
    public void KnownGrowthTableWithoutRetention_IsReported()
    {
        var snapshot = Snapshot(
            tables: [Table("CommandLog", rows: 5_000_000, bytes: Mb)],
            retention: new RetentionSpec(PurgeEnabled: false, [], []));

        Assert.Contains(Run(snapshot), f => f.RuleId == TableBloatRule.RetentionId);
    }

    [Fact]
    public void KnownGrowthTableCoveredByRetention_IsNotReported()
    {
        var snapshot = Snapshot(
            tables: [Table("CommandLog", rows: 5_000_000, bytes: Mb)],
            retention: new RetentionSpec(PurgeEnabled: true, ["/System/Log"], ["CommandLog"]));

        Assert.DoesNotContain(Run(snapshot), f => f.RuleId == TableBloatRule.RetentionId);
    }

    [Fact]
    public void PurgeOnButTableNotListed_IsStillReported()
    {
        var snapshot = Snapshot(
            tables: [Table("CommandLog", rows: 5_000_000, bytes: Mb)],
            retention: new RetentionSpec(PurgeEnabled: true, ["/System/Log"], ["GeneralLog"]));

        var finding = Run(snapshot).Single(f => f.RuleId == TableBloatRule.RetentionId);
        Assert.Contains("not in the database retention table list", finding.Detail);
    }

    [Fact]
    public void BusinessTable_IsNotTreatedAsALogTable()
    {
        var snapshot = Snapshot(
            tables: [Table("EcomOrders", rows: 5_000_000, bytes: Mb)],
            retention: new RetentionSpec(PurgeEnabled: false, [], []));

        Assert.DoesNotContain(Run(snapshot), f => f.RuleId == TableBloatRule.RetentionId);
    }

    [Fact]
    public void SmallLogTable_IsBelowTheRowFloor()
    {
        var snapshot = Snapshot(
            tables: [Table("CommandLog", rows: 1_000, bytes: Mb)],
            retention: new RetentionSpec(PurgeEnabled: false, [], []));

        Assert.Empty(Run(snapshot));
    }
}
