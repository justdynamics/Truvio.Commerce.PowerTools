using Truvio.Commerce.PowerTools.Core.Operations;
using Truvio.Commerce.PowerTools.Core.Operations.Rules;
using Xunit;
using static Truvio.Commerce.PowerTools.Tests.OperationsTestData;

namespace Truvio.Commerce.PowerTools.Tests;

public class OpsFormatTests
{
    [Theory]
    [InlineData("Dynamicweb.DataIntegration.Integration.JobScheduledTaskAddIn, Dynamicweb.DataIntegration", "JobScheduledTaskAddIn")]
    [InlineData("Dynamicweb.Scheduling.ScheduledTaskAddIns.MethodScheduledTaskAddIn", "MethodScheduledTaskAddIn")]
    [InlineData("BareType", "BareType")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void ShortTypeName_StripsNamespaceAndAssembly(string? input, string expected) =>
        Assert.Equal(expected, OpsFormat.ShortTypeName(input));

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    public void Bytes_UsesBinaryUnits(long input, string expected) =>
        Assert.Equal(expected, OpsFormat.Bytes(input));

    [Fact]
    public void Relative_HandlesPastPresentFutureAndNever()
    {
        Assert.Equal("never", OpsFormat.Relative(null, Now));
        Assert.Equal("just now", OpsFormat.Relative(Now.AddSeconds(-5), Now));
        Assert.Equal("5 min ago", OpsFormat.Relative(Now.AddMinutes(-5), Now));
        Assert.Equal("3 h ago", OpsFormat.Relative(Now.AddHours(-3), Now));
        Assert.Equal("2 d ago", OpsFormat.Relative(Now.AddDays(-2), Now));
        Assert.Equal("in 4 h", OpsFormat.Relative(Now.AddHours(4), Now));
    }

    [Fact]
    public void Duration_ScalesFromSubSecondToHours()
    {
        Assert.Equal("-", OpsFormat.Duration(null));
        Assert.Equal("0.4 s", OpsFormat.Duration(TimeSpan.FromMilliseconds(400)));
        Assert.Equal("12 s", OpsFormat.Duration(TimeSpan.FromSeconds(12)));
        Assert.Equal("3 m 05 s", OpsFormat.Duration(TimeSpan.FromSeconds(185)));
        Assert.Equal("1 h 12 m", OpsFormat.Duration(TimeSpan.FromMinutes(72)));
    }

    [Theory]
    [InlineData(0, "once")]
    [InlineData(5, "every 5 min")]
    [InlineData(60, "every 1 h")]
    [InlineData(90, "every 1 h 30 min")]
    [InlineData(1440, "every 1 d")]
    [InlineData(2880, "every 2 d")]
    public void Interval_ReadsAsAPhrase(int minutes, string expected) =>
        Assert.Equal(expected, OpsFormat.Interval(minutes));
}

public class ActivityLinkTests
{
    [Theory]
    [InlineData("Nightly\\Import", "Nightly\\Import")]
    [InlineData("Nightly/Import", "Nightly\\Import")]
    [InlineData("  \\Import\\  ", "Import")]
    [InlineData(null, "")]
    public void Normalise_UnifiesSeparatorsAndTrims(string? input, string expected) =>
        Assert.Equal(expected, ActivityLinks.Normalise(input));

    [Fact]
    public void TasksFor_FindsEveryTaskRunningTheActivity()
    {
        var activity = Activity("Import Customers", group: "Nightly");
        var tasks = new[]
        {
            Task(id: 1, linkedActivityId: "Nightly\\Import Customers"),
            Task(id: 2, linkedActivityId: "nightly/import customers"),
            Task(id: 3, linkedActivityId: "Other")
        };

        var linked = ActivityLinks.TasksFor(tasks, activity);

        Assert.Equal([1, 2], linked.Select(t => t.Id).Order());
    }
}

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
        public IEnumerable<Truvio.Commerce.PowerTools.Core.Diagnostics.Finding> Evaluate(OperationsSnapshot snapshot) =>
            throw new InvalidOperationException("boom");
    }
}
