using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.PimQuality.PimTestData;
using Truvio.Commerce.PowerTools.Features.PimQuality.Core;
using Truvio.Commerce.PowerTools.Features.PimQuality.Core.Rules;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Tests.Features.PimQuality;

// ---- PIM-W4 variant combinations ---------------------------------------------------------------

public class VariantGapRuleTests
{
    private static IReadOnlyList<Finding> Run(PimSnapshot snapshot) =>
        new VariantGapRule().Evaluate(snapshot).ToList();

    [Fact]
    public void Missing_combinations_are_reported_with_examples()
    {
        var findings = Run(Snapshot(gaps: [Gap(potential: 12, existing: 8, examples: ["RED.XL", "BLUE.S"])]));

        var finding = Assert.Single(findings);
        Assert.Equal(VariantGapRule.Id, finding.RuleId);
        Assert.Contains("4 of 12", finding.Title);
        Assert.Contains("RED.XL", finding.Detail);
    }

    [Fact]
    public void A_complete_combination_space_is_not_a_gap()
    {
        Assert.Empty(Run(Snapshot(gaps: [Gap(potential: 8, existing: 8)])));
    }

    [Fact]
    public void A_huge_combination_space_is_reported_as_a_number_not_a_list()
    {
        var finding = Assert.Single(Run(Snapshot(gaps: [Gap(potential: 100_000, existing: 3, examples: [])])));

        Assert.Contains("Too many potential combinations", finding.Detail);
        Assert.Contains("99,997", finding.Title);
    }

    [Fact]
    public void Existing_beyond_potential_never_reports_a_negative_gap()
    {
        // DW's potential count is a ceiling; a stale count must not underflow the subtraction.
        Assert.Empty(Run(Snapshot(gaps: [Gap(potential: 4, existing: 9)])));
    }
}
