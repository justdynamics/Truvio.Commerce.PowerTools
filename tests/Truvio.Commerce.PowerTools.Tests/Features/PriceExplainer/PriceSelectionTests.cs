using Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;
using Xunit;

namespace Truvio.Commerce.PowerTools.Tests.Features.PriceExplainer;

public class PriceSelectionTests
{
    private static PriceRowSpec Row(string id, double amount, double discount = 0, int priority = 0) =>
        new() { Id = id, Amount = amount, DiscountPercentage = discount, Priority = priority };

    [Fact]
    public void Higher_priority_beats_a_cheaper_net_price()
    {
        var result = PriceRowEvaluator.Evaluate([Row("cheap", 10, 50), Row("priority", 100, 0, 1)],
            new(), PriceSelectionRule.PriorityThenLowestNet);
        Assert.Equal("priority", result.Winner!.Row.Id);
        Assert.True(result.Rows[0].IsOutranked);
        Assert.Null(result.Rows[0].ShadowedBy);
    }

    [Fact]
    public void Lowest_net_wins_instead_of_lowest_gross()
    {
        var result = PriceRowEvaluator.Evaluate([Row("gross", 80), Row("net", 100, 30)],
            new(), PriceSelectionRule.PriorityThenLowestNet);
        Assert.Equal("net", result.Winner!.Row.Id);
        Assert.Equal(70, result.Winner.NetAmount, 6);
    }

    [Theory]
    [InlineData(PriceSelectionRule.LowestNet, 5)]
    [InlineData(PriceSelectionRule.PriorityThenLowestNet, 5)]
    [InlineData(PriceSelectionRule.GroupsPriorityThenLowestNet, 20)]
    public void Own_discount_or_higher_fallback_depends_on_rule(PriceSelectionRule rule, double expected)
    {
        var result = PriceRowEvaluator.Evaluate([Row("amount", 100, 5), Row("discount", 0, 20)], new(), rule);
        Assert.Equal(expected, result.Winner!.EffectiveDiscountPercentage);
        Assert.True(result.Rows[1].IsDiscountLine);
        Assert.False(result.Rows[1].IsWinner);
    }

    [Fact]
    public void Groups_fallback_includes_percentage_on_amount_rows()
    {
        var rows = new[] { Row("one", 100, 5, 1), Row("two", 200, 30) };
        Assert.Equal(5, PriceRowEvaluator.Evaluate(rows, new(), PriceSelectionRule.PriorityThenLowestNet).Winner!.EffectiveDiscountPercentage);
        Assert.Equal(30, PriceRowEvaluator.Evaluate(rows, new(), PriceSelectionRule.GroupsPriorityThenLowestNet).Winner!.EffectiveDiscountPercentage);
    }

    [Fact]
    public void Disallowed_line_discounts_are_zero_even_with_fallback()
    {
        var result = PriceRowEvaluator.Evaluate([Row("amount", 100, 40) with { AllowOrderLineDiscounts = false }, Row("discount", 0, 50)],
            new(), PriceSelectionRule.GroupsPriorityThenLowestNet);
        Assert.Equal(0, result.Winner!.EffectiveDiscountPercentage);
        Assert.Equal(100, result.Winner.NetAmount);
    }

    [Fact]
    public void Only_discount_rows_leave_product_default_with_fallback()
    {
        var result = PriceRowEvaluator.Evaluate([Row("discount", 0, 20)], new(), PriceSelectionRule.PriorityThenLowestNet);
        Assert.Null(result.Winner);
        Assert.Equal(20, result.FallbackDiscountPercentage);
    }

    [Fact]
    public void Old_net_rule_ignores_priority_and_new_rule_ignores_nonpositive_priority()
    {
        Assert.Equal("cheap", PriceRowEvaluator.Evaluate([Row("cheap", 10), Row("priority", 100, 0, 5)],
            new(), PriceSelectionRule.LowestNet).Winner!.Row.Id);
        Assert.Equal("cheap", PriceRowEvaluator.Evaluate([Row("cheap", 10, 0, -1), Row("zero", 100)],
            new(), PriceSelectionRule.PriorityThenLowestNet).Winner!.Row.Id);
    }

    [Fact]
    public void Ties_keep_the_authoritative_enumeration_order()
    {
        var result = PriceRowEvaluator.Evaluate([Row("first", 100, 20), Row("second", 80)],
            new(), PriceSelectionRule.PriorityThenLowestNet);
        Assert.True(result.HasTie);
        Assert.Equal("first", result.Winner!.Row.Id);
    }

    [Fact]
    public void Vat_normalization_precedes_net_comparison()
    {
        var result = PriceRowEvaluator.Evaluate([Row("vat", 121, 20) with { IsWithVat = true }, Row("net", 90)],
            new() { VatPercent = 21 }, PriceSelectionRule.PriorityThenLowestNet);
        Assert.Equal("vat", result.Winner!.Row.Id);
        Assert.Equal(80, result.Winner.NetAmount, 6);
    }

    [Fact]
    public void Unknown_or_custom_rule_never_classifies_a_winner_or_discount_line()
    {
        var result = PriceRowEvaluator.Evaluate([Row("a", 0, 20), Row("b", 100)], new(), PriceSelectionRule.Unknown);
        Assert.Null(result.Winner);
        Assert.All(result.Rows, r => { Assert.True(r.Matches); Assert.False(r.IsDiscountLine); Assert.False(r.IsOutranked); });
    }

    [Fact]
    public void Dw_matching_set_overrides_mirror_in_both_directions()
    {
        var result = PriceRowEvaluator.Evaluate([Row("accepted", 10) with { UserId = "9" }, Row("rejected", 5)],
            new(), PriceSelectionRule.LowestAmount, new HashSet<string> { "accepted" });
        Assert.Equal("accepted", result.Winner!.Row.Id);
        Assert.Equal("Rejected by DW", result.Rows[1].RejectionReason);
    }

    [Fact]
    public void Groups_product_and_valid_configuration_are_checked()
    {
        var row = Row("group", 0, 20) with { ProductDiscountGroupId = "P", CustomerDiscountGroupId = "C" };
        Assert.True(PriceRowEvaluator.Evaluate(row, new() { ProductDiscountGroupId = "P", CustomerDiscountGroupId = "C" }).Matches);
        Assert.False(PriceRowEvaluator.Evaluate(row, new()).Matches);
        Assert.False(PriceRowEvaluator.Evaluate(row with { HasValidConfiguration = false }, new()).Matches);
        Assert.False(PriceRowEvaluator.Evaluate(Row("price", 10) with { CustomerPriceGroupId = "A" }, new()).Matches);
        Assert.False(PriceRowEvaluator.Evaluate(Row("product", 10) with { ProductId = "other" }, new() { ProductId = "selected" }).Matches);
    }

    [Fact]
    public void Higher_quantity_is_only_a_step_for_the_same_account()
    {
        Assert.True(PriceRowEvaluator.Evaluate(Row("break", 10) with { Quantity = 10 }, new()).NeedsHigherQuantity);
        Assert.False(PriceRowEvaluator.Evaluate(Row("other", 10) with { Quantity = 10, UserId = "9" }, new()).NeedsHigherQuantity);
    }

    [Fact]
    public void Large_rejected_set_is_summarized_without_losing_matches()
    {
        var rows = Enumerable.Range(1, 322000).Select(i => Row(i.ToString(), 10) with { UserId = i.ToString() });
        var result = PriceRowEvaluator.Evaluate(rows, new() { UserId = 17 }, PriceSelectionRule.PriorityThenLowestNet);
        Assert.Equal(1, result.MatchCount);
        Assert.Equal(321999, result.RejectedCounts["Other customers"]);
        Assert.Equal("17", result.Winner!.Row.Id);
    }

    [Theory]
    [InlineData("10.8.4", false, false, PriceSelectionRule.LowestAmount)]
    [InlineData("10.17.5", true, false, PriceSelectionRule.LowestNet)]
    [InlineData("10.28.17", true, false, PriceSelectionRule.PriorityThenLowestNet)]
    [InlineData("10.30.6", true, true, PriceSelectionRule.GroupsPriorityThenLowestNet)]
    [InlineData("10.30.6", true, false, PriceSelectionRule.Unknown)]
    [InlineData("10.28.17", false, false, PriceSelectionRule.Unknown)]
    [InlineData("10.99.1", true, true, PriceSelectionRule.Unknown)]
    [InlineData("10.8.5", false, false, PriceSelectionRule.Unknown)]
    public void Host_rules_are_exact_and_api_gated(string version, bool discounts, bool groups, PriceSelectionRule expected)
        => Assert.Equal(expected, PriceRuleVersions.Resolve(Version.Parse(version), discounts, groups));

}
