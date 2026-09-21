using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.PimQuality.PimTestData;
using Truvio.Commerce.PowerTools.Features.PimQuality.Core;
using Truvio.Commerce.PowerTools.Features.PimQuality.Core.Rules;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Tests.Features.PimQuality;

// ---- PIM-W3 language layers ------------------------------------------------------------------

public class LanguageGapRuleTests
{
    private static IReadOnlyList<Finding> Run(PimSnapshot snapshot, int points = 20) =>
        new LanguageGapRule(points).Evaluate(snapshot).ToList();

    private static ProductQuality Bilingual(string id, int english, int danish) =>
        Product(productId: id, number: id, perLanguage: new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["LANG1"] = english,
            ["LANG2"] = danish
        });

    [Fact]
    public void Language_materially_behind_is_reported()
    {
        var snapshot = Snapshot(
            products: [Bilingual("A", 100, 40), Bilingual("B", 100, 40)],
            languages: ["LANG1", "LANG2"]);

        var finding = Assert.Single(Run(snapshot));
        Assert.Equal(LanguageGapRule.Id, finding.RuleId);
        Assert.Equal("LANG2", finding.EntityKey);
        Assert.Contains("60 points behind", finding.Title);
    }

    [Fact]
    public void Language_within_tolerance_stays_quiet()
    {
        var snapshot = Snapshot(
            products: [Bilingual("A", 100, 90)],
            languages: ["LANG1", "LANG2"]);

        Assert.Empty(Run(snapshot));
    }

    [Fact]
    public void The_default_language_is_never_reported_against_itself()
    {
        var snapshot = Snapshot(
            products: [Bilingual("A", 10, 10)],
            languages: ["LANG1"]);

        Assert.Empty(Run(snapshot));
    }

    [Fact]
    public void Products_without_both_layers_are_skipped()
    {
        var snapshot = Snapshot(
            products: [Product(perLanguage: new Dictionary<string, int> { ["LANG1"] = 100 })],
            languages: ["LANG1", "LANG2"]);

        Assert.Empty(Run(snapshot));
    }
}
