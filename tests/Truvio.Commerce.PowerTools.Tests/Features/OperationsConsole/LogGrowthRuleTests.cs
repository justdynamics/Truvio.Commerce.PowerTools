using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.OperationsConsole.OperationsTestData;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Core;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Core.Rules;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Tests.Features.OperationsConsole;

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
