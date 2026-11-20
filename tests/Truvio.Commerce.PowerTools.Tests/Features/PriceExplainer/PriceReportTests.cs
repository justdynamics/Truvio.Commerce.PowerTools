using Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Queries;
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Screens;
using Xunit;

namespace Truvio.Commerce.PowerTools.Tests.Features.PriceExplainer;

public class PriceReportTests
{
    [Theory]
    [InlineData(int.MaxValue, 0)]
    [InlineData(0, 0)]
    [InlineData(10, 10)]
    [InlineData(int.MaxValue - 1, int.MaxValue - 1)]
    public void Discount_priority_displays_configured_value_without_changing_sort_priority(int priority, int displayed)
    {
        var discount = new DiscountSpec { Id = 7, Name = "Campaign", Priority = priority };
        var report = new ExplainReport
        {
            Discounts = [new DiscountExplanation { Verdict = new DiscountVerdict { Discount = discount } }]
        };
        var rows = PriceExplainQuery.ToRows(report);
        var row = Assert.Single(rows, r => r.Section == "Discounts" && r.Item.StartsWith("Campaign"));
        Assert.Equal($"Campaign (#7, prio {displayed})", row.Item);
        Assert.Equal(priority, report.Discounts[0].Verdict.Discount.Priority);
    }

    [Theory]
    [InlineData(PriceSelectionRule.LowestAmount, "Lowest matching price")]
    [InlineData(PriceSelectionRule.LowestNet, "Lowest net price after found discount")]
    [InlineData(PriceSelectionRule.PriorityThenLowestNet, "Highest priority, then lowest net price")]
    [InlineData(PriceSelectionRule.GroupsPriorityThenLowestNet, "Highest priority, then lowest net price (including price and discount groups)")]
    [InlineData(PriceSelectionRule.Unknown, "Unverified price selection")]
    [InlineData((PriceSelectionRule)999, "Unverified price selection")]
    public void Selection_rule_description_uses_readable_text_including_unknown_rules(PriceSelectionRule rule, string expected)
    {
        var description = PriceRuleDescriptions.Describe(rule);
        Assert.Equal(expected, description);
        Assert.DoesNotContain(rule.ToString(), description);
    }

    [Fact]
    public void Applicable_rows_precede_collapsible_rejections_and_each_list_is_bounded()
    {
        var specs = Enumerable.Range(1, 50).Select(i => new PriceRowSpec { Id = $"other-{i}", Amount = 1, UserId = "99" })
            .Append(new PriceRowSpec { Id = "mine", Amount = 100 });
        var matrix = PriceRowEvaluator.Evaluate(specs, new() { UserId = 1 }, PriceSelectionRule.PriorityThenLowestNet);
        var rows = PriceExplainQuery.ToRows(new ExplainReport { PriceMatrix = matrix }, 3);
        var evidence = rows.Where(r => r.Section == "Price matrix" && !r.IsHeader).ToList();
        Assert.Contains(evidence, r => r.Item.StartsWith("Row mine") && r.Verdict == "Wins");
        var rejected = Assert.Single(evidence, r => r.Item == "Other customers");
        Assert.Equal("50 rows", rejected.Value);
        Assert.Equal(3, rejected.DetailRows.Count);
        Assert.True(evidence.FindIndex(r => r.Item.StartsWith("Row mine")) < evidence.IndexOf(rejected));
    }

    [Fact]
    public void Unknown_rule_uses_applies_instead_of_shadowed_or_wins()
    {
        var matrix = PriceRowEvaluator.Evaluate([new PriceRowSpec { Id = "x", Amount = 10 }], new(), PriceSelectionRule.Unknown);
        var rows = PriceExplainQuery.ToRows(new ExplainReport { PriceMatrix = matrix });
        var row = Assert.Single(rows, r => r.Item.StartsWith("Row x"));
        Assert.Equal("Applies", row.Verdict);
        Assert.Contains("unavailable", row.Why);
        Assert.DoesNotContain("own 0%", row.Why);
    }

    [Fact]
    public void Steps_render_four_values_and_source_with_escaped_content()
    {
        var html = PriceExplainScreen.RenderSteps([new(10, "100", "15% / 15", "5", "80", "<script>bad</script>")]);
        Assert.Contains("Found price", html);
        Assert.Contains("Found discount", html);
        Assert.Contains("Campaign discounts", html);
        Assert.Contains("Final unit price", html);
        Assert.Contains("Source", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.DoesNotContain("<script>", html);
    }

    [Fact]
    public void Context_switch_preserves_all_other_price_dimensions()
    {
        var query = new PriceExplainQuery { AccountKey = "7", ProductId = "P", VariantId = "V", LanguageId = "L", CurrencyCode = "EUR", CountryCode = "DK", ShopId = "S", Quantity = 10, Date = "2026-11-20" };
        var changed = PriceExplainScreen.Copy(query, q => q.Quantity = 50);
        Assert.Equal(50, changed.Quantity);
        Assert.Equal(query.AccountKey, changed.AccountKey);
        Assert.Equal(query.ProductId, changed.ProductId);
        Assert.Equal(query.VariantId, changed.VariantId);
        Assert.Equal(query.LanguageId, changed.LanguageId);
        Assert.Equal(query.CurrencyCode, changed.CurrencyCode);
        Assert.Equal(query.CountryCode, changed.CountryCode);
        Assert.Equal(query.ShopId, changed.ShopId);
        Assert.Equal(query.Date, changed.Date);
    }
}
