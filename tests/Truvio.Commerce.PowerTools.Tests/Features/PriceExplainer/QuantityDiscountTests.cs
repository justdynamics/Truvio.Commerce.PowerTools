using Dynamicweb.Ecommerce.International;
using Dynamicweb.Ecommerce.Prices;
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Dw;
using Xunit;

namespace Truvio.Commerce.PowerTools.Tests.Features.PriceExplainer;

public class QuantityDiscountTests
{
    private static readonly Currency Currency = new() { Code = "EUR" };

    private static PriceInfo Price(double amount) => new(Currency)
    {
        PriceWithoutVAT = amount, PriceWithVAT = amount * 1.25, VAT = amount * .25, VATPercent = 25
    };

    [Theory]
    [InlineData(1, 100, 80, 80)]
    [InlineData(10, 80, 64, 640)]
    [InlineData(50, 60, 48, 2400)]
    public void Host_ladder_preserves_additive_savings_and_line_totals(double quantity, double found, double final, double total)
    {
        foreach (var rule in new[] { PriceSelectionRule.PriorityThenLowestNet, PriceSelectionRule.GroupsPriorityThenLowestNet })
        {
            var rows = new[]
            {
                new PriceRowSpec { Id = "A", Quantity = 1, Amount = 100 },
                new PriceRowSpec { Id = "B", Quantity = 10, Amount = 80 },
                new PriceRowSpec { Id = "C", Quantity = 50, Amount = 60 },
                new PriceRowSpec { Id = "discount", DiscountPercentage = 15 }
            };
            var winner = PriceRowEvaluator.Evaluate(rows, new() { Quantity = quantity }, rule).Winner!;
            Assert.Equal(found, winner.ComparableAmount);
            var price = Price(winner.ComparableAmount);
            var result = DwCampaignPrices.Combine(price, winner.EffectiveDiscountPercentage,
                new Dictionary<int, IPriceInfo> { [1] = DwCampaignPrices.Percentage(price, 5, null) }, "acc");
            Assert.Equal(final, result.Final.PriceWithoutVAT, 6);
            Assert.Equal(final * 1.25, result.Final.PriceWithVAT, 6);
            Assert.Equal(found * .15, result.FoundDiscount.PriceWithoutVAT, 6);
            Assert.Equal(total, result.Final.Multiply(quantity).PriceWithoutVAT, 6);
        }
    }

    [Theory]
    [InlineData(PriceSelectionRule.PriorityThenLowestNet, true, 99)]
    [InlineData(PriceSelectionRule.GroupsPriorityThenLowestNet, true, 88)]
    [InlineData(PriceSelectionRule.PriorityThenLowestNet, false, 110)]
    [InlineData(PriceSelectionRule.GroupsPriorityThenLowestNet, false, 110)]
    public void Host_priority_row_respects_the_version_rule_and_allow_flag(PriceSelectionRule rule, bool allow, double final)
    {
        var winner = PriceRowEvaluator.Evaluate([
            new PriceRowSpec { Id = "cheap", Amount = 60 },
            new PriceRowSpec { Id = "D", Amount = 110, Priority = 10, DiscountPercentage = 5, AllowOrderLineDiscounts = allow },
            new PriceRowSpec { Id = "discount", DiscountPercentage = 15 }], new(), rule).Winner!;
        Assert.Equal("D", winner.Row.Id);
        var price = Price(winner.ComparableAmount);
        var result = DwCampaignPrices.Combine(price, winner.EffectiveDiscountPercentage,
            allow ? new Dictionary<int, IPriceInfo> { [1] = DwCampaignPrices.Percentage(price, 5, null) } : new Dictionary<int, IPriceInfo>(), "acc");
        Assert.Equal(final, result.Final.PriceWithoutVAT, 6);
        if (!allow) Assert.Equal(0, result.Campaigns.PriceWithoutVAT);
    }

    [Theory]
    [InlineData("acc", 12.5, 920)]
    [InlineData("high", 7, 975)]
    [InlineData("low", 5.5, 990)]
    public void Host_fixed_and_percentage_selection_preserves_totals_and_applied_evidence(string behavior, double campaign, double total)
    {
        var price = Price(110);
        var result = DwCampaignPrices.Combine(price, 5,
            new Dictionary<int, IPriceInfo> { [1] = DwCampaignPrices.Percentage(price, 5, null), [2] = Price(7) }, behavior);
        Assert.Equal(campaign, result.Campaigns.PriceWithoutVAT);
        Assert.Equal(total, result.Final.Multiply(10).PriceWithoutVAT, 6);
        Assert.Equal(behavior == "acc" ? 2 : 1, result.Applied.Count);
        if (behavior == "high") Assert.Contains(2, result.Applied.Keys);
        if (behavior == "low") Assert.Contains(1, result.Applied.Keys);
    }

    [Theory]
    [InlineData(110, 5, 5.6, 934)] // DW returns a fixed 7 incl. VAT as 5.60 excl.
    [InlineData(72, 0, 0, 684)] // DW converts the stored default 90 at rate 125 to 72.
    public void Host_vat_and_converted_inputs_preserve_line_totals(double found, double percentage, double fixedAmount, double total)
    {
        var price = Price(found);
        var result = DwCampaignPrices.Combine(price, percentage,
            new Dictionary<int, IPriceInfo> { [1] = DwCampaignPrices.Percentage(price, 5, null), [2] = Price(fixedAmount) }, "acc");
        Assert.Equal(total, result.Final.Multiply(10).PriceWithoutVAT, 6);
        Assert.Equal(total * 1.25, result.Final.Multiply(10).PriceWithVAT, 6);
    }

    [Fact]
    public void Rounding_uses_the_dw_overload_without_rounding_the_found_price_first()
    {
        var price = Price(10.01);
        var unrounded = DwCampaignPrices.Percentage(price, 15, null);
        var rounded = DwCampaignPrices.Percentage(price, 15, 2);
        Assert.Equal(1.5015, unrounded.PriceWithoutVAT, 6);
        Assert.Equal(1.876875, unrounded.PriceWithVAT, 6);
        Assert.Equal(1.88, rounded.PriceWithVAT, 6);
    }

    [Fact]
    public void Nonpositive_price_percentage_does_not_create_a_surcharge()
    {
        var result = DwCampaignPrices.Combine(Price(100), -10, new Dictionary<int, IPriceInfo>(), "acc");
        Assert.Equal(0, result.FoundDiscount.PriceWithoutVAT);
        Assert.Equal(100, result.Final.PriceWithoutVAT);
    }

    [Fact]
    public void Missing_percentage_stays_unknown_in_the_result()
    {
        var result = DwCampaignPrices.Combine(Price(100), null, new Dictionary<int, IPriceInfo>(), "acc");
        Assert.Null(result.Percentage);
    }
}
