using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.OperationsConsole.OperationsTestData;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Core;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Core.Rules;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Tests.Features.OperationsConsole;

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
