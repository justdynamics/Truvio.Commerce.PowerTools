using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.PimQuality.PimTestData;
using Truvio.Commerce.PowerTools.Features.PimQuality.Core;
using Truvio.Commerce.PowerTools.Features.PimQuality.Core.Rules;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Tests.Features.PimQuality;

// ---- PIM-W1 incomplete products -----------------------------------------------------------

public class IncompleteProductRuleTests
{
    private static IReadOnlyList<Finding> Run(PimSnapshot snapshot, int threshold = 60) =>
        new IncompleteProductRule(threshold).Evaluate(snapshot).ToList();

    [Fact]
    public void Product_below_threshold_is_reported()
    {
        var findings = Run(Snapshot(products: [Product(score: 40, missing: ["ShortDescription"])]));

        var finding = Assert.Single(findings);
        Assert.Equal(IncompleteProductRule.Id, finding.RuleId);
        Assert.Equal(FindingSeverity.Warning, finding.Severity);
        Assert.Contains("40%", finding.Title);
        Assert.Contains("ShortDescription", finding.Detail);
    }

    [Fact]
    public void Product_at_the_threshold_is_not_reported()
    {
        Assert.Empty(Run(Snapshot(products: [Product(score: 60)])));
    }

    [Fact]
    public void Score_of_zero_is_critical_not_warning()
    {
        var finding = Assert.Single(Run(Snapshot(products: [Product(score: 0)])));

        Assert.Equal(FindingSeverity.Critical, finding.Severity);
    }

    [Fact]
    public void Worst_products_come_first()
    {
        var findings = Run(Snapshot(products:
        [
            Product(productId: "A", number: "A", score: 50),
            Product(productId: "B", number: "B", score: 10),
            Product(productId: "C", number: "C", score: 30)
        ]));

        Assert.Equal(["B", "C", "A"], findings.Select(f => f.EntityKey));
    }

    [Fact]
    public void A_product_with_no_named_missing_field_still_explains_itself()
    {
        var finding = Assert.Single(Run(Snapshot(products: [Product(score: 20, missing: [])])));

        Assert.Contains("no specific missing field", finding.Detail);
    }
}
