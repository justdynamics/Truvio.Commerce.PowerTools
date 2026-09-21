using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.OperationsConsole.OperationsTestData;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Core;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Core.Rules;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Tests.Features.OperationsConsole;

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
