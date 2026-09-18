using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.OperationsConsole.OperationsTestData;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Core;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Core.Rules;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Tests.Features.OperationsConsole;

public class OperationsHealthEngineTests
{
    [Fact]
    public void Summarise_CountsEachProblemOnce()
    {
        var snapshot = Snapshot(
            tasks:
            [
                Task(id: 1, name: "Failing", lastRun: Now.AddMinutes(-5), lastResult: false),
                Task(id: 2, name: "Stale", intervalMinutes: 60, lastRun: Now.AddDays(-3)),
                Task(id: 3, name: "Broken", lastRun: Now.AddMinutes(-1), linkedActivityId: "Gone"),
                Task(id: 4, name: "Fine", lastRun: Now.AddMinutes(-1))
            ],
            activities: [Activity("Import Customers", lastRun: Now.AddDays(-1))]);

        var health = new OperationsHealthEngine().Summarise(snapshot);

        Assert.Equal(4, health.TaskCount);
        Assert.Equal(4, health.EnabledTaskCount);
        Assert.Equal(1, health.FailingTaskCount);
        Assert.Equal(1, health.StaleTaskCount);
        Assert.Equal(1, health.BrokenLinkCount);
        Assert.Equal("Attention needed", health.Verdict);
    }

    [Fact]
    public void Summarise_OnACleanInstall_ReportsHealthy()
    {
        var snapshot = Snapshot(tasks: [Task(lastRun: Now.AddMinutes(-1), linkedActivityId: "Import Customers")],
                                activities: [Activity("Import Customers", lastRun: Now.AddMinutes(-1))]);

        var health = new OperationsHealthEngine().Summarise(snapshot);

        Assert.Empty(health.Findings);
        Assert.Equal("Healthy", health.Verdict);
    }

    [Fact]
    public void Run_OrdersWorstFirst()
    {
        var snapshot = Snapshot(
            tasks: [Task(id: 1, lastRun: Now.AddMinutes(-5), lastResult: false)],
            activities: [Activity("Unscheduled")]);

        var findings = new OperationsHealthEngine().Run(snapshot);

        Assert.Equal(FailingTaskRule.Id, findings[0].RuleId);
        Assert.Equal(BrokenActivityLinkRule.UnscheduledId, findings[^1].RuleId);
    }

    [Fact]
    public void Run_KeepsGoingWhenARuleThrows()
    {
        var findings = new OperationsHealthEngine([new ThrowingRule(), new FailingTaskRule()])
            .Run(Snapshot(tasks: [Task(lastRun: Now.AddMinutes(-5), lastResult: false)]));

        Assert.Contains(findings, f => f.RuleId == FailingTaskRule.Id);
        Assert.Contains(findings, f => f.RuleId == "OPS-E1");
    }

    private sealed class ThrowingRule : IOperationsRule
    {
        public IEnumerable<Finding> Evaluate(OperationsSnapshot snapshot) =>
            throw new InvalidOperationException("boom");
    }
}
