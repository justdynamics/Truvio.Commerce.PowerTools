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
        var description = PriceRuleDescriptions.Describe(new PriceMatrixVerdict { Rule = rule });
        Assert.StartsWith(expected, description);
        Assert.DoesNotContain(rule.ToString(), description);
    }

    [Fact]
    public void Found_discount_without_fallback_has_no_empty_source_clause()
    {
        var matrix = PriceRowEvaluator.Evaluate([new PriceRowSpec { Id = "PRICE1", Amount = 100 }], new(), PriceSelectionRule.PriorityThenLowestNet);
        var text = PriceRuleDescriptions.Describe(matrix);
        Assert.Contains("no fallback row. Row PRICE1: own 0%, effective 0%.", text);
        Assert.DoesNotContain("from matching rows", text);
    }

    [Theory]
    [InlineData(PriceSelectionRule.PriorityThenLowestNet, 5)]
    [InlineData(PriceSelectionRule.GroupsPriorityThenLowestNet, 15)]
    public void Found_discount_keeps_bounded_fallback_sources_and_the_host_rule(PriceSelectionRule rule, int effective)
    {
        var rows = Enumerable.Range(1, 6).Select(i => new PriceRowSpec { Id = $"D{i}", DiscountPercentage = 15 })
            .Prepend(new PriceRowSpec { Id = "P", Amount = 110, DiscountPercentage = 5 });
        var text = PriceRuleDescriptions.Describe(PriceRowEvaluator.Evaluate(rows, new(), rule));
        Assert.Contains("fallback 15% from matching rows D1, D2, D3, D4, D5 (more sources omitted)", text);
        Assert.DoesNotContain("D6", text);
        Assert.Contains($"Row P: own 5%, effective {effective}%.", text);
    }

    [Fact]
    public void Price_section_keeps_all_figures_and_default_row_identifies_stored_currency()
    {
        var rows = PriceExplainQuery.ToRows(new ExplainReport
        {
            DwPriceBeforeDiscount = "72 EUR", FoundDiscount = "0%", DwDiscountTotal = "3.60 EUR",
            DwFinalPrice = "68.40 EUR", LineTotal = "684 EUR", ProductDefaultPrice = "90 USD"
        });
        Assert.Equal(new[] { "72 EUR", "0%", "3.60 EUR", "68.40 EUR", "684 EUR" },
            rows.Where(r => r.Section == "Price" && !r.IsHeader).Select(r => r.Value));
        var stored = Assert.Single(rows, r => r.Item == "Product default price");
        Assert.Equal("90 USD", stored.Value);
        Assert.Contains("Stored amount in the default currency, before conversion", stored.Why);
    }

    [Fact]
    public void Steps_collapse_identical_consecutive_prices_but_keep_changes_and_returning_prices()
    {
        var first = new PriceStep(1, "110", "15% / 16.50", "5.50", "88", "Product (row D)");
        var same = PriceExplainScreen.RenderSteps([first, first with { Quantity = 10 }, first with { Quantity = 50 }]);
        Assert.Single(System.Xml.Linq.XElement.Parse(same).Descendants("tbody").Single().Elements("tr"));
        Assert.Contains("Quantity from", same);
        var changed = new[]
        {
            first with { Quantity = 10, Source = "Product (row E)" },
            first with { Quantity = 10, FoundPrice = "100" },
            first with { Quantity = 10, FoundDiscount = "5% / 5.50" },
            first with { Quantity = 10, CampaignDiscounts = "7" },
            first with { Quantity = 10, FinalPrice = "80" }
        };
        foreach (var step in changed)
        {
            var html = PriceExplainScreen.RenderSteps([first, step, first with { Quantity = 50 }]);
            Assert.Equal(3, System.Xml.Linq.XElement.Parse(html).Descendants("tbody").Single().Elements("tr").Count());
        }
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
