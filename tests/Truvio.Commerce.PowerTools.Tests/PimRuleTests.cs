using Truvio.Commerce.PowerTools.Core.Diagnostics;
using Truvio.Commerce.PowerTools.Core.Pim;
using Truvio.Commerce.PowerTools.Core.Pim.Rules;
using Xunit;
using static Truvio.Commerce.PowerTools.Tests.PimTestData;

namespace Truvio.Commerce.PowerTools.Tests;

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
        var findings = new PimQualityEngine(Truvio.Commerce.PowerTools.Core.Settings.PowerToolsSettings.Defaults)
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
        var findings = new PimQualityEngine(Truvio.Commerce.PowerTools.Core.Settings.PowerToolsSettings.Defaults)
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
        var quality = new PimQualityEngine(Truvio.Commerce.PowerTools.Core.Settings.PowerToolsSettings.Defaults)
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
        var quality = new PimQualityEngine(Truvio.Commerce.PowerTools.Core.Settings.PowerToolsSettings.Defaults)
            .Summarise(Snapshot(products: [Product(score: 100)], languages: ["LANG1"]));

        Assert.True(quality.Healthy);
        Assert.Equal("Healthy", quality.Verdict);
    }

    private sealed class ThrowingRule : IPimRule
    {
        public IEnumerable<Finding> Evaluate(PimSnapshot snapshot) => throw new InvalidOperationException("boom");
    }
}

// ---- The snapshot's own arithmetic ---------------------------------------------------------------------

public class PimSnapshotTests
{
    [Fact]
    public void Truncation_is_derived_from_the_total_count()
    {
        var snapshot = Snapshot(products: [Product()], totalProductCount: 900);

        Assert.True(snapshot.IsTruncated);
        Assert.Equal(899, snapshot.NotShownCount);
    }

    [Fact]
    public void A_complete_scan_is_not_truncated()
    {
        var snapshot = Snapshot(products: [Product()], totalProductCount: 1);

        Assert.False(snapshot.IsTruncated);
        Assert.Equal(0, snapshot.NotShownCount);
    }

    [Fact]
    public void Average_of_an_empty_catalog_is_zero_not_a_crash()
    {
        Assert.Equal(0, Snapshot().AverageScore);
    }

    [Fact]
    public void Scope_falls_back_when_the_cap_is_nonsense()
    {
        Assert.Equal(PimScope.DefaultProductCap, new PimScope(ProductCap: 0).EffectiveCap);
        Assert.Equal(PimScope.DefaultProductCap, new PimScope(ProductCap: -5).EffectiveCap);
        Assert.Equal(50, new PimScope(ProductCap: 50).EffectiveCap);
    }
}
