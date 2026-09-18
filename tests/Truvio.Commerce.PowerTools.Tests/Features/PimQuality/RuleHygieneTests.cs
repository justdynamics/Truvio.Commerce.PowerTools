using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.PimQuality.PimTestData;
using Truvio.Commerce.PowerTools.Features.PimQuality.Core.Rules;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Tests.Features.PimQuality;

// ---- PIM-W6 / PIM-W7 hygiene -----------------------------------------------------------------------

public class RuleHygieneTests
{
    [Fact]
    public void Rule_assigned_to_nothing_is_dead()
    {
        var findings = new DeadCompletionRuleRule()
            .Evaluate(Snapshot(rules: [Rule(name: "Print ready", usages: [])])).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal(DeadCompletionRuleRule.Id, finding.RuleId);
        Assert.Equal(FindingSeverity.Warning, finding.Severity);
        Assert.Equal("Print ready", finding.EntityDisplayName);
        Assert.Contains("scores nothing", finding.Detail);
    }

    [Fact]
    public void An_assigned_rule_is_not_reported()
    {
        Assert.Empty(new DeadCompletionRuleRule()
            .Evaluate(Snapshot(rules: [Rule(usages: ["Shop 'Northwind'"])])));
    }

    [Fact]
    public void Category_used_by_no_group_is_reported()
    {
        var findings = new UnusedCategoryRule()
            .Evaluate(Snapshot(categories: [Category(name: "Legacy", groupCount: 0)])).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal(UnusedCategoryRule.Id, finding.RuleId);
        Assert.Equal(FindingSeverity.Info, finding.Severity);
    }

    [Fact]
    public void A_used_category_is_not_reported()
    {
        Assert.Empty(new UnusedCategoryRule().Evaluate(Snapshot(categories: [Category(groupCount: 3)])));
    }
}
