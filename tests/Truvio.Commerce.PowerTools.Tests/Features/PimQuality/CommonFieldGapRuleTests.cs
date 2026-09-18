using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.PimQuality.PimTestData;
using Truvio.Commerce.PowerTools.Features.PimQuality.Core;
using Truvio.Commerce.PowerTools.Features.PimQuality.Core.Rules;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Tests.Features.PimQuality;

// ---- PIM-W2 the field to fix first ----------------------------------------------------------

public class CommonFieldGapRuleTests
{
    private static IReadOnlyList<Finding> Run(PimSnapshot snapshot, int percent = 25) =>
        new CommonFieldGapRule(percent).Evaluate(snapshot).ToList();

    private static PimSnapshot ThreeProducts(params string[][] missingPerProduct) =>
        Snapshot(products: missingPerProduct
            .Select((missing, i) => Product(productId: $"P{i}", number: $"P{i}", score: 50, missing: missing))
            .ToList());

    [Fact]
    public void Field_missing_on_most_products_is_reported_with_its_share()
    {
        var findings = Run(ThreeProducts(["Weight"], ["Weight"], ["Weight"]));

        var finding = Assert.Single(findings);
        Assert.Equal(CommonFieldGapRule.Id, finding.RuleId);
        Assert.Equal(FindingSeverity.Info, finding.Severity);
        Assert.Contains("3 of 3", finding.Title);
        Assert.Contains("100", finding.Title);
    }

    [Fact]
    public void Field_below_the_share_threshold_stays_quiet()
    {
        // One product of four = 25%, threshold 50.
        var snapshot = ThreeProducts(["Weight"], [], [], []);

        Assert.Empty(Run(snapshot, percent: 50));
    }

    [Fact]
    public void Empty_catalog_reports_nothing()
    {
        Assert.Empty(Run(Snapshot()));
    }

    [Fact]
    public void Ranking_puts_the_most_common_field_first()
    {
        var ranked = CommonFieldGapRule.Rank(ThreeProducts(
            ["Weight", "Colour"],
            ["Weight"],
            ["Weight"]));

        Assert.Equal("Weight", ranked[0].Field);
        Assert.Equal(3, ranked[0].Count);
        Assert.Equal("Colour", ranked[1].Field);
    }

    [Fact]
    public void The_same_field_twice_on_one_product_counts_once()
    {
        var ranked = CommonFieldGapRule.Rank(ThreeProducts(["Weight", "weight"]));

        var entry = Assert.Single(ranked);
        Assert.Equal(1, entry.Count);
    }

    [Fact]
    public void Truncated_scan_says_the_percentage_is_of_the_sample()
    {
        var snapshot = Snapshot(
            products: [Product(score: 10, missing: ["Weight"])],
            totalProductCount: 900);

        var finding = Assert.Single(Run(snapshot));
        Assert.Contains("not the whole catalog", finding.Detail);
    }
}
