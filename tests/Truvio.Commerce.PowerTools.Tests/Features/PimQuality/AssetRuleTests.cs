using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.PimQuality.PimTestData;
using Truvio.Commerce.PowerTools.Features.PimQuality.Core.Rules;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Tests.Features.PimQuality;

// ---- PIM-W5 / PIM-W8 assets ----------------------------------------------------------------------

public class AssetRuleTests
{
    [Fact]
    public void Duplicate_asset_is_info_and_names_the_path()
    {
        var findings = new DuplicateAssetRule()
            .Evaluate(Snapshot(duplicates: [Duplicate(path: "/Files/a.jpg", count: 3)])).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal(DuplicateAssetRule.Id, finding.RuleId);
        Assert.Equal(FindingSeverity.Info, finding.Severity);
        Assert.Contains("/Files/a.jpg", finding.Detail);
        Assert.Equal("/Files/a.jpg", finding.Subject);
    }

    [Fact]
    public void A_single_attachment_is_not_a_duplicate()
    {
        Assert.Empty(new DuplicateAssetRule().Evaluate(Snapshot(duplicates: [Duplicate(count: 1)])));
    }

    [Fact]
    public void Broken_image_is_a_warning_and_names_the_path()
    {
        var findings = new BrokenImageRule()
            .Evaluate(Snapshot(broken: [Broken(path: "/Files/missing.png")])).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal(BrokenImageRule.Id, finding.RuleId);
        Assert.Equal(FindingSeverity.Warning, finding.Severity);
        Assert.Contains("/Files/missing.png", finding.Detail);
    }
}
