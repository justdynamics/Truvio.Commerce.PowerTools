using Dynamicweb.Ecommerce.International;
using Dynamicweb.Ecommerce.Prices;
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
    [InlineData(100, 80, 100)]
    [InlineData(80, 64, 80)]
    public void Price_line_and_campaign_savings_use_the_same_quantity_price_and_are_additive(double found, double finalExcl, double finalIncl)
    {
        var price = Price(found);
        var campaign = DwCampaignPrices.Percentage(price, 5, null);
        var result = DwCampaignPrices.Combine(price, 15, new Dictionary<int, IPriceInfo> { [1] = campaign }, "acc");
        Assert.Equal(finalExcl, result.Final.PriceWithoutVAT, 6);
        Assert.Equal(finalIncl, result.Final.PriceWithVAT, 6);
        Assert.Equal(found * .15, result.FoundDiscount.PriceWithoutVAT, 6);
        Assert.Equal(finalExcl * 10, result.Final.Multiply(10).PriceWithoutVAT, 6);
    }

    [Theory]
    [InlineData("acc", 25, 65)]
    [InlineData("high", 20, 70)]
    [InlineData("low", 5, 85)]
    public void Selection_affects_both_campaign_total_and_applied_evidence(string behavior, double campaign, double final)
    {
        var result = DwCampaignPrices.Combine(Price(100), 10,
            new Dictionary<int, IPriceInfo> { [1] = Price(5), [2] = Price(20) }, behavior);
        Assert.Equal(campaign, result.Campaigns.PriceWithoutVAT);
        Assert.Equal(final, result.Final.PriceWithoutVAT);
        Assert.Equal(behavior == "acc" ? 2 : 1, result.Applied.Count);
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
