using Truvio.Commerce.PowerTools.Core.Diagnostics;
using Truvio.Commerce.PowerTools.Core.Operations;
using Truvio.Commerce.PowerTools.Core.Operations.Rules;
using Xunit;
using static Truvio.Commerce.PowerTools.Tests.OperationsTestData;

namespace Truvio.Commerce.PowerTools.Tests;

public class FailingTaskRuleTests
{
    private static IReadOnlyList<Finding> Run(OperationsSnapshot snapshot) =>
        new FailingTaskRule().Evaluate(snapshot).ToList();

    [Fact]
    public void FalseLastResult_IsCritical()
    {
        var findings = Run(Snapshot(tasks: [Task(lastRun: Now.AddMinutes(-10), lastResult: false)]));

        var finding = Assert.Single(findings);
        Assert.Equal(FailingTaskRule.Id, finding.RuleId);
        Assert.Equal(FindingSeverity.Critical, finding.Severity);
    }

    [Fact]
    public void StoredException_IsCritical_EvenWhenResultSaysTrue()
    {
        var findings = Run(Snapshot(tasks: [Task(lastRun: Now.AddMinutes(-10), lastResult: true, lastException: "Timeout")]));

        var finding = Assert.Single(findings);
        Assert.Contains("Timeout", finding.Detail);
    }

    [Fact]
    public void DisabledTask_IsNotReported()
    {
        var findings = Run(Snapshot(tasks: [Task(enabled: false, lastRun: Now.AddMinutes(-10), lastResult: false)]));

        Assert.Empty(findings);
    }

    [Fact]
    public void NeverRunTask_IsNotAFailure()
    {
        var findings = Run(Snapshot(tasks: [Task(lastRun: null, lastResult: null)]));

        Assert.Empty(findings);
    }

    [Fact]
    public void HealthyTask_ProducesNothing()
    {
        var findings = Run(Snapshot(tasks: [Task(lastRun: Now.AddMinutes(-5))]));

        Assert.Empty(findings);
    }
}

public class StaleTaskRuleTests
{
    private static IReadOnlyList<Finding> Run(OperationsSnapshot snapshot) =>
        new StaleTaskRule().Evaluate(snapshot).ToList();

    [Fact]
    public void LateButWithinTwoIntervals_IsNotStale()
    {
        // 60-minute task, last run 100 minutes ago: late, but not yet twice the interval.
        var findings = Run(Snapshot(tasks: [Task(intervalMinutes: 60, lastRun: Now.AddMinutes(-100))]));

        Assert.Empty(findings);
    }

    [Fact]
    public void BeyondTwoIntervals_IsStale()
    {
        var findings = Run(Snapshot(tasks: [Task(intervalMinutes: 60, lastRun: Now.AddMinutes(-125))]));

        var finding = Assert.Single(findings);
        Assert.Equal(StaleTaskRule.StaleId, finding.RuleId);
        Assert.Equal(FindingSeverity.Warning, finding.Severity);
    }

    [Fact]
    public void ExactlyTwoIntervals_IsNotYetStale()
    {
        var findings = Run(Snapshot(tasks: [Task(intervalMinutes: 60, lastRun: Now.AddMinutes(-120))]));

        Assert.Empty(findings);
    }

    [Fact]
    public void NeverRun_GetsItsOwnRule()
    {
        var findings = Run(Snapshot(tasks: [Task(lastRun: null)]));

        var finding = Assert.Single(findings);
        Assert.Equal(StaleTaskRule.NeverRunId, finding.RuleId);
    }

    [Fact]
    public void OneOffTask_IsNeverStale()
    {
        // A task with no repeat interval that has run once is finished, not stale.
        var findings = Run(Snapshot(tasks: [Task(intervalMinutes: 0, lastRun: Now.AddYears(-2))]));

        Assert.Empty(findings);
    }

    [Fact]
    public void DisabledTask_IsNeverStale()
    {
        var findings = Run(Snapshot(tasks: [Task(enabled: false, lastRun: Now.AddYears(-2))]));

        Assert.Empty(findings);
    }
}

public class BrokenActivityLinkRuleTests
{
    private static IReadOnlyList<Finding> Run(OperationsSnapshot snapshot) =>
        new BrokenActivityLinkRule().Evaluate(snapshot).ToList();

    [Fact]
    public void TaskPointingAtMissingActivity_IsCritical()
    {
        var snapshot = Snapshot(
            tasks: [Task(linkedActivityId: "Nightly\\Import Products")],
            activities: [Activity("Import Customers")]);

        var broken = Run(snapshot).Single(f => f.RuleId == BrokenActivityLinkRule.BrokenId);
        Assert.Equal(FindingSeverity.Critical, broken.Severity);
        Assert.Contains("Import Products", broken.Title);
    }

    [Fact]
    public void MatchingActivity_IsNotBroken()
    {
        var snapshot = Snapshot(
            tasks: [Task(linkedActivityId: "Nightly\\Import Customers")],
            activities: [Activity("Import Customers", group: "Nightly")]);

        Assert.DoesNotContain(Run(snapshot), f => f.RuleId == BrokenActivityLinkRule.BrokenId);
    }

    [Fact]
    public void IdentifierSeparatorAndCase_DoNotMatter()
    {
        var snapshot = Snapshot(
            tasks: [Task(linkedActivityId: "nightly/import customers")],
            activities: [Activity("Import Customers", group: "Nightly")]);

        Assert.DoesNotContain(Run(snapshot), f => f.RuleId == BrokenActivityLinkRule.BrokenId);
    }

    [Fact]
    public void NoActivitiesAtAll_DowngradesToWarning()
    {
        var snapshot = Snapshot(tasks: [Task(linkedActivityId: "Import Customers")], activities: []);

        var broken = Run(snapshot).Single(f => f.RuleId == BrokenActivityLinkRule.BrokenId);
        Assert.Equal(FindingSeverity.Warning, broken.Severity);
    }

    [Fact]
    public void ActivityWithoutAnyTask_IsInformational()
    {
        var snapshot = Snapshot(tasks: [], activities: [Activity("Import Customers")]);

        var finding = Assert.Single(Run(snapshot));
        Assert.Equal(BrokenActivityLinkRule.UnscheduledId, finding.RuleId);
        Assert.Equal(FindingSeverity.Info, finding.Severity);
    }

    [Fact]
    public void ActivityScheduledOnlyByDisabledTask_CountsAsUnscheduled()
    {
        var snapshot = Snapshot(
            tasks: [Task(enabled: false, linkedActivityId: "Import Customers")],
            activities: [Activity("Import Customers")]);

        Assert.Contains(Run(snapshot), f => f.RuleId == BrokenActivityLinkRule.UnscheduledId);
    }

    [Fact]
    public void ManyUnscheduledActivities_CollapseIntoOneFinding()
    {
        var activities = Enumerable.Range(1, BrokenActivityLinkRule.AggregateThreshold + 1)
            .Select(i => Activity($"Import {i}"))
            .ToList();

        var finding = Assert.Single(Run(Snapshot(activities: activities)));
        Assert.Equal(BrokenActivityLinkRule.UnscheduledId, finding.RuleId);
        Assert.Contains($"{activities.Count} activities", finding.EntityDisplayName);
        Assert.Contains("Import 1", finding.Detail);
    }

    [Fact]
    public void FewUnscheduledActivities_StayIndividual()
    {
        var activities = Enumerable.Range(1, BrokenActivityLinkRule.AggregateThreshold)
            .Select(i => Activity($"Import {i}"))
            .ToList();

        Assert.Equal(activities.Count, Run(Snapshot(activities: activities)).Count);
    }

    [Fact]
    public void TaskWithNoActivityParameter_IsIgnored()
    {
        var snapshot = Snapshot(tasks: [Task(linkedActivityId: "")], activities: []);

        Assert.Empty(Run(snapshot));
    }
}

public class LogGrowthRuleTests
{
    private static IReadOnlyList<Finding> Run(OperationsSnapshot snapshot) =>
        new LogGrowthRule().Evaluate(snapshot).ToList();

    [Fact]
    public void FolderOverTwoGigabytes_IsCritical()
    {
        var findings = Run(Snapshot(folders: [Folder(bytes: 3L * 1024 * 1024 * 1024)]));

        var size = findings.Single(f => f.RuleId == LogGrowthRule.SizeId);
        Assert.Equal(FindingSeverity.Critical, size.Severity);
    }

    [Fact]
    public void FolderOverHalfAGigabyte_IsWarning()
    {
        var findings = Run(Snapshot(folders: [Folder(bytes: 700L * 1024 * 1024)]));

        var size = findings.Single(f => f.RuleId == LogGrowthRule.SizeId);
        Assert.Equal(FindingSeverity.Warning, size.Severity);
    }

    [Fact]
    public void SmallFolder_IsIgnored()
    {
        Assert.DoesNotContain(Run(Snapshot(folders: [Folder(bytes: 4096)])), f => f.RuleId == LogGrowthRule.SizeId);
    }

    [Fact]
    public void LongHistoryWithPurgingOff_IsReported()
    {
        var snapshot = Snapshot(
            folders: [Folder(fileCount: 90, oldest: Now.AddDays(-90), newest: Now)],
            retention: new RetentionSpec(PurgeEnabled: false, [], []));

        Assert.Contains(Run(snapshot), f => f.RuleId == LogGrowthRule.RetentionId);
    }

    [Fact]
    public void LongHistoryWithPurgingOn_IsNotReported()
    {
        var snapshot = Snapshot(
            folders: [Folder(fileCount: 90, oldest: Now.AddDays(-90), newest: Now)],
            retention: new RetentionSpec(PurgeEnabled: true, ["/System/Log"], []));

        Assert.DoesNotContain(Run(snapshot), f => f.RuleId == LogGrowthRule.RetentionId);
    }

    [Fact]
    public void SingleFileFolder_IsNotCalledUnrotated()
    {
        var snapshot = Snapshot(
            folders: [Folder(fileCount: 1, oldest: Now.AddDays(-400), newest: Now)],
            retention: new RetentionSpec(PurgeEnabled: false, [], []));

        Assert.DoesNotContain(Run(snapshot), f => f.RuleId == LogGrowthRule.RetentionId);
    }
}

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
