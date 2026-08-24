using Truvio.Commerce.PowerTools.Core.Diagnostics;
using Truvio.Commerce.PowerTools.Core.Pim;
using Truvio.Commerce.PowerTools.Core.Pim.Rules;
using Truvio.Commerce.PowerTools.Core.Settings;
using Xunit;
using static Truvio.Commerce.PowerTools.Tests.PimTestData;

namespace Truvio.Commerce.PowerTools.Tests;

/// <summary>
/// The PIM thresholds the settings screen exposes really do change what the rules report — and
/// a nonsense value falls back to the shipped default instead of disabling the rule. Same
/// contract as <see cref="ConfigurableThresholdTests"/> for the Operations rules.
/// </summary>
public class PimThresholdTests
{
    // ---- PIM-W1 completeness threshold ------------------------------------------------------

    [Fact]
    public void Completeness_threshold_decides_what_counts_as_incomplete()
    {
        var snapshot = Snapshot(products: [Product(score: 55)]);

        Assert.Single(new IncompleteProductRule(60).Evaluate(snapshot));
        Assert.Empty(new IncompleteProductRule(50).Evaluate(snapshot));
    }

    [Fact]
    public void Completeness_threshold_falls_back_when_stored_as_zero()
    {
        // 0 would report nothing at all; the shipped 60 must win.
        var settings = PowerToolsSettings.Defaults with { PimCompletenessThreshold = 0 };

        Assert.Equal(PimQualityEngine.DefaultThreshold, PimQualityEngine.Incomplete(settings).Threshold);
    }

    [Fact]
    public void Completeness_threshold_from_settings_reaches_the_rule()
    {
        var settings = PowerToolsSettings.Defaults with { PimCompletenessThreshold = 90 };

        Assert.Equal(90, PimQualityEngine.Incomplete(settings).Threshold);
    }

    // ---- PIM-W2 common-gap percentage --------------------------------------------------------

    [Fact]
    public void Common_gap_threshold_decides_which_fields_are_reported()
    {
        // One product of four missing Weight = 25%.
        var snapshot = Snapshot(products:
        [
            Product(productId: "A", number: "A", missing: ["Weight"]),
            Product(productId: "B", number: "B"),
            Product(productId: "C", number: "C"),
            Product(productId: "D", number: "D")
        ]);

        Assert.Single(new CommonFieldGapRule(25).Evaluate(snapshot));
        Assert.Empty(new CommonFieldGapRule(50).Evaluate(snapshot));
    }

    [Fact]
    public void Common_gap_threshold_falls_back_when_stored_as_zero()
    {
        var settings = PowerToolsSettings.Defaults with { PimCommonGapPercent = 0 };

        Assert.Equal(PowerToolsSettingKeys.Defaults.PimCommonGapPercent,
            PimQualityEngine.CommonFieldGap(settings).PercentThreshold);
    }

    // ---- PIM-W3 language tolerance -------------------------------------------------------------

    [Fact]
    public void Language_tolerance_is_derived_from_the_completeness_threshold()
    {
        // Accepting 60% completeness accepts a 40-point spread between layers.
        var settings = PowerToolsSettings.Defaults with { PimCompletenessThreshold = 60 };
        Assert.Equal(40, PimQualityEngine.LanguagePointsThreshold(settings));

        // A strict install tolerates less drift, but never less than 5 points.
        var strict = PowerToolsSettings.Defaults with { PimCompletenessThreshold = 99 };
        Assert.Equal(5, PimQualityEngine.LanguagePointsThreshold(strict));
    }

    // ---- Suppression -----------------------------------------------------------------------------

    [Fact]
    public void Suppressed_rule_ids_are_hidden_but_counted()
    {
        var settings = PowerToolsSettings.Defaults with { PimSuppressedRules = IncompleteProductRule.Id };
        var findings = new PimQualityEngine(settings).Run(Snapshot(products: [Product(score: 10, missing: ["Weight"])]));

        var filtered = settings.FilterPimFindings(findings);

        Assert.DoesNotContain(filtered.Visible, f => f.RuleId == IncompleteProductRule.Id);
        Assert.True(filtered.HiddenCount > 0);
        Assert.Contains("hidden by settings", filtered.HiddenNotice());
    }

    [Fact]
    public void A_prefix_token_suppresses_a_whole_rule_family()
    {
        var settings = PowerToolsSettings.Defaults with { PimSuppressedRules = "PIM-W*" };
        var findings = new PimQualityEngine(settings).Run(Snapshot(
            products: [Product(score: 10, missing: ["Weight"])],
            rules: [Rule(usages: [])]));

        Assert.Empty(settings.FilterPimFindings(findings).Visible);
    }

    [Fact]
    public void Suppressing_one_rule_leaves_the_others_visible()
    {
        var settings = PowerToolsSettings.Defaults with { PimSuppressedRules = DeadCompletionRuleRule.Id };
        var findings = new PimQualityEngine(settings).Run(Snapshot(
            products: [Product(score: 10, missing: ["Weight"])],
            rules: [Rule(usages: [])]));

        var visible = settings.FilterPimFindings(findings).Visible;

        Assert.DoesNotContain(visible, f => f.RuleId == DeadCompletionRuleRule.Id);
        Assert.Contains(visible, f => f.RuleId == IncompleteProductRule.Id);
    }

    // ---- The shipped engine ------------------------------------------------------------------------

    [Fact]
    public void Default_settings_reproduce_the_shipped_engine()
    {
        var snapshot = Snapshot(
            products: [Product(score: 10, missing: ["Weight"])],
            rules: [Rule(usages: [])],
            languages: ["LANG1"]);

        var shipped = new PimQualityEngine().Run(snapshot).Select(f => f.RuleId).ToList();
        var configured = new PimQualityEngine(PowerToolsSettings.Defaults).Run(snapshot).Select(f => f.RuleId).ToList();

        Assert.Equal(shipped, configured);
    }

    // ---- The scan cap travels to the source ----------------------------------------------------------

    [Fact]
    public void The_configured_cap_reaches_the_source()
    {
        var source = new FakePimQualitySource { Products = { Product() } };

        source.Snapshot(new PimScope(ProductCap: 42));

        Assert.Equal(42, Assert.Single(source.RequestedScopes).EffectiveCap);
    }

    [Fact]
    public void Skipping_the_catalog_wide_passes_leaves_only_scores()
    {
        var source = new FakePimQualitySource
        {
            Products = { Product() },
            Gaps = { Gap() },
            BrokenImages = { Broken() },
            Categories = { Category(groupCount: 0) }
        };

        var snapshot = source.Snapshot(PimScope.Default, includeCatalogWide: false);

        Assert.NotEmpty(snapshot.Products);
        Assert.Empty(snapshot.VariantGaps);
        Assert.Empty(snapshot.BrokenImages);
        Assert.Empty(snapshot.Categories);
    }

    [Fact]
    public void Findings_carry_the_severity_the_screens_colour_by()
    {
        var findings = new PimQualityEngine(PowerToolsSettings.Defaults)
            .Run(Snapshot(products: [Product(score: 0)], languages: ["LANG1"]));

        Assert.Contains(findings, f => f.Severity == FindingSeverity.Critical);
    }
}
