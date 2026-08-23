using Truvio.Commerce.PowerTools.Core.Commerce;
using Xunit;

namespace Truvio.Commerce.PowerTools.Tests;

public class DiscountCandidateEvaluatorTests
{
    private static readonly DateTime Now = new(2026, 8, 23, 12, 0, 0);

    private static DiscountLookupContext User(params int[] groups) => new()
    {
        UserId = 17,
        UserCustomerNumber = "C-100",
        UserGroupIds = groups.ToHashSet(),
        CurrencyCode = "EUR",
        CountryCode = "NL",
        ShopId = "SHOP1",
        LanguageId = "LANG1",
        Time = Now
    };

    private static readonly DiscountLookupContext Anonymous = User() with { UserId = null, UserCustomerNumber = null };

    private static DiscountSpec Discount() => new() { Id = 1, Name = "d", Active = true };

    [Fact]
    public void Open_active_discount_passes()
    {
        Assert.True(DiscountCandidateEvaluator.Evaluate(Discount(), User()).PassesBaseChecks);
    }

    [Fact]
    public void Anonymous_only_discount_rejects_signed_in_users_and_accepts_anonymous()
    {
        var d = Discount() with { AnonymousUsers = true };

        Assert.False(DiscountCandidateEvaluator.Evaluate(d, User()).PassesBaseChecks);
        Assert.True(DiscountCandidateEvaluator.Evaluate(d, Anonymous).PassesBaseChecks);
    }

    [Fact]
    public void Group_user_and_customer_number_targeting_are_explained()
    {
        var byGroup = Discount() with { UserGroupId = 5 };
        var byUser = Discount() with { UserId = 99 };
        var byNumber = Discount() with { UserCustomerNumber = "C-200" };

        Assert.Contains(DiscountCandidateEvaluator.Evaluate(byGroup, User(6)).FailedChecks, f => f.Contains("group 5"));
        Assert.True(DiscountCandidateEvaluator.Evaluate(byGroup, User(5)).PassesBaseChecks);
        Assert.Contains(DiscountCandidateEvaluator.Evaluate(byUser, User()).FailedChecks, f => f.Contains("user id 99"));
        Assert.Contains(DiscountCandidateEvaluator.Evaluate(byNumber, User()).FailedChecks, f => f.Contains("C-200"));
    }

    [Fact]
    public void Validity_currency_shop_country_language_are_checked()
    {
        var d = Discount() with
        {
            ValidTo = Now.AddDays(-1),
            CurrencyCode = "USD",
            ShopId = "SHOP2",
            CountryCode = "DE",
            LanguageId = "LANG2"
        };

        var verdict = DiscountCandidateEvaluator.Evaluate(d, User());

        Assert.Equal(5, verdict.FailedChecks.Count);
    }

    [Fact]
    public void MinValue_dates_count_as_unrestricted()
    {
        var d = Discount() with { ValidFrom = DateTime.MinValue, ValidTo = DateTime.MinValue };

        Assert.True(DiscountCandidateEvaluator.Evaluate(d, User()).PassesBaseChecks);
    }

    [Fact]
    public void Cart_dependent_conditions_are_reported_not_guessed()
    {
        var d = Discount() with { NeedsOrder = true, NeedsOrderReason = "order total ≥ 500" };

        var verdict = DiscountCandidateEvaluator.Evaluate(d, User());

        Assert.Contains(verdict.FailedChecks, f => f.Contains("Needs a cart") && f.Contains("500"));
    }
}
