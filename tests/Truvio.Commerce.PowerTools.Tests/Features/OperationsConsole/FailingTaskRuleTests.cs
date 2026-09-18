using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.OperationsConsole.OperationsTestData;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Core;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Core.Rules;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Tests.Features.OperationsConsole;

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
