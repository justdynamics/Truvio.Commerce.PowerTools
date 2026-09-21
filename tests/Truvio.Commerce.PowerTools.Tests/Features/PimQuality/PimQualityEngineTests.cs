using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.PimQuality.PimTestData;
using Truvio.Commerce.PowerTools.Features.PimQuality.Core;
using Truvio.Commerce.PowerTools.Features.PimQuality.Core.Rules;
using Truvio.Commerce.PowerTools.Features.Settings.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Tests.Features.PimQuality;

// ---- The engine ---------------------------------------------------------------------------------------

public class PimQualityEngineTests
{
    private static PimSnapshot Everything() => Snapshot(
        products: [Product(productId: "A", number: "A", score: 20, missing: ["Weight"])],
        rules: [Rule(name: "Dead one", usages: [])],
        gaps: [Gap(potential: 10, existing: 2)],
        duplicates: [Duplicate()],
        broken: [Broken()],
        categories: [Category(groupCount: 0)],
        languages: ["LANG1"]);

    [Fact]
    public void Every_rule_contributes_to_one_ordered_list()
    {
        var findings = new PimQualityEngine(PowerToolsSettings.Defaults)
            .Run(Everything());

        var ids = findings.Select(f => f.RuleId).Distinct().ToList();
        Assert.Contains(IncompleteProductRule.Id, ids);
        Assert.Contains(CommonFieldGapRule.Id, ids);
        Assert.Contains(VariantGapRule.Id, ids);
        Assert.Contains(DuplicateAssetRule.Id, ids);
        Assert.Contains(DeadCompletionRuleRule.Id, ids);
        Assert.Contains(UnusedCategoryRule.Id, ids);
        Assert.Contains(BrokenImageRule.Id, ids);
    }

    [Fact]
    public void Findings_are_ordered_worst_first()
    {
        var findings = new PimQualityEngine(PowerToolsSettings.Defaults)
            .Run(Everything());

        var severities = findings.Select(f => f.Severity).ToList();
        Assert.Equal(severities.OrderByDescending(s => s), severities);
    }

    [Fact]
    public void A_throwing_rule_does_not_hide_the_others()
    {
        var engine = new PimQualityEngine([new ThrowingRule(), new DeadCompletionRuleRule()]);

        var findings = engine.Run(Snapshot(rules: [Rule(usages: [])]));

        Assert.Contains(findings, f => f.RuleId == "PIM-E1");
        Assert.Contains(findings, f => f.RuleId == DeadCompletionRuleRule.Id);
    }

    [Fact]
    public void Summary_counts_what_the_infobar_shows()
    {
        var quality = new PimQualityEngine(PowerToolsSettings.Defaults)
            .Summarise(Everything());

        Assert.Equal(1, quality.ProductsScanned);
        Assert.Equal(20, quality.AverageScore);
        Assert.Equal(1, quality.BelowThresholdCount);
        Assert.Equal(1, quality.VariantGapCount);
        Assert.Equal(1, quality.BrokenImageCount);
        Assert.Equal(1, quality.DeadRuleCount);
        Assert.Equal("Weight", quality.WorstField);
        Assert.False(quality.Healthy);
        // Score 20 is a Warning; only an empty product (score 0) escalates to Critical.
        Assert.Equal("Needs a look", quality.Verdict);
    }

    [Fact]
    public void A_clean_catalog_reports_healthy()
    {
        var quality = new PimQualityEngine(PowerToolsSettings.Defaults)
            .Summarise(Snapshot(products: [Product(score: 100)], languages: ["LANG1"]));

        Assert.True(quality.Healthy);
        Assert.Equal("Healthy", quality.Verdict);
    }

    private sealed class ThrowingRule : IPimRule
    {
        public IEnumerable<Finding> Evaluate(PimSnapshot snapshot) => throw new InvalidOperationException("boom");
    }
}
