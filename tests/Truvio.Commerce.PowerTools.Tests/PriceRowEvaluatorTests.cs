using Truvio.Commerce.PowerTools.Core.Commerce;
using Xunit;

namespace Truvio.Commerce.PowerTools.Tests;

public class PriceRowEvaluatorTests
{
    private static readonly DateTime Now = new(2026, 8, 23, 12, 0, 0);

    private static PriceLookupContext Context(
        int? userId = 17,
        string currency = "EUR",
        double quantity = 1,
        params int[] groups) => new()
    {
        UserId = userId,
        UserCustomerNumber = "C-100",
        UserGroupIds = groups.ToHashSet(),
        UserGroupCustomerNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CG-1" },
        CurrencyCode = currency,
        CountryCode = "NL",
        ShopId = "SHOP1",
        LanguageId = "LANG1",
        VariantId = "",
        UnitId = "PCS",
        Quantity = quantity,
        Time = Now,
        VatPercent = 21,
        PricesInDatabaseIncludeVat = false
    };

    private static PriceRowSpec Row(string id, double amount) => new() { Id = id, Amount = amount };

    [Fact]
    public void Unrestricted_row_matches_and_lists_no_restrictions()
    {
        var verdict = PriceRowEvaluator.Evaluate(Row("1", 10), Context());

        Assert.True(verdict.Matches);
        Assert.Empty(verdict.SatisfiedRestrictions);
        Assert.Equal(10, verdict.ComparableAmount);
    }

    [Fact]
    public void Cheapest_matching_row_wins_regardless_of_specificity()
    {
        // DW's DefaultPriceProvider takes MinBy(amount) — a broad cheap row beats a specific dear one.
        var rows = new[]
        {
            Row("broad", 8),
            Row("group", 9) with { UserGroupId = "5" }
        };

        var result = PriceRowEvaluator.Evaluate(rows, Context(groups: 5));

        Assert.Equal("broad", result.Winner!.Row.Id);
        var shadowed = result.Rows.Single(r => r.Row.Id == "group");
        Assert.True(shadowed.Matches);
        Assert.Equal(1, shadowed.ShadowedBy);
    }

    [Fact]
    public void Group_row_is_rejected_for_non_members_with_reason()
    {
        var verdict = PriceRowEvaluator.Evaluate(Row("g", 5) with { UserGroupId = "5" }, Context(groups: 6));

        Assert.False(verdict.Matches);
        Assert.Contains(verdict.FailedChecks, f => f.Contains("group 5"));
    }

    [Fact]
    public void UserGroupId_takes_precedence_over_legacy_CustomerGroupId()
    {
        // The legacy column is only consulted when UserGroupId is blank (CustomerGroupPriceFilter).
        var legacyOnly = Row("legacy", 5) with { CustomerGroupId = "CG-1" };
        var both = Row("both", 5) with { UserGroupId = "99", CustomerGroupId = "CG-1" };

        Assert.True(PriceRowEvaluator.Evaluate(legacyOnly, Context()).Matches);
        Assert.False(PriceRowEvaluator.Evaluate(both, Context()).Matches);
    }

    [Fact]
    public void UserId_takes_precedence_over_customer_number()
    {
        var byNumber = Row("n", 5) with { UserCustomerNumber = "C-100" };
        var byIdAndNumber = Row("i", 5) with { UserId = "99", UserCustomerNumber = "C-100" };

        Assert.True(PriceRowEvaluator.Evaluate(byNumber, Context()).Matches);
        Assert.False(PriceRowEvaluator.Evaluate(byIdAndNumber, Context()).Matches);
    }

    [Fact]
    public void Quantity_threshold_applies_and_any_variant_rows_may_use_cross_variant_quantity()
    {
        var tier = Row("tier", 5) with { Quantity = 10 };
        Assert.False(PriceRowEvaluator.Evaluate(tier, Context(quantity: 9)).Matches);
        Assert.True(PriceRowEvaluator.Evaluate(tier, Context(quantity: 10)).Matches);

        var anyVariantTier = tier with { VariantId = "Any" };
        var ctx = Context(quantity: 2) with { QuantityAllVariants = 12 };
        Assert.True(PriceRowEvaluator.Evaluate(anyVariantTier, ctx).Matches);
    }

    [Fact]
    public void Validity_window_is_checked_against_context_time()
    {
        var future = Row("f", 5) with { ValidFrom = Now.AddDays(1) };
        var expired = Row("e", 5) with { ValidTo = Now.AddDays(-1) };
        var open = Row("o", 5) with { ValidFrom = Now.AddDays(-1), ValidTo = Now.AddDays(1) };

        Assert.False(PriceRowEvaluator.Evaluate(future, Context()).Matches);
        Assert.False(PriceRowEvaluator.Evaluate(expired, Context()).Matches);
        Assert.True(PriceRowEvaluator.Evaluate(open, Context()).Matches);
    }

    [Fact]
    public void Currency_country_shop_language_and_unit_restrictions_are_explained()
    {
        var row = Row("x", 5) with
        {
            CurrencyCode = "USD",
            CountryCode = "DE",
            ShopId = "SHOP2",
            LanguageId = "LANG2",
            UnitId = "BOX"
        };

        var verdict = PriceRowEvaluator.Evaluate(row, Context());

        Assert.Equal(5, verdict.FailedChecks.Count);
        Assert.Contains(verdict.FailedChecks, f => f.StartsWith("Currency USD"));
        Assert.Contains(verdict.FailedChecks, f => f.StartsWith("Country DE"));
        Assert.Contains(verdict.FailedChecks, f => f.StartsWith("Shop SHOP2"));
        Assert.Contains(verdict.FailedChecks, f => f.StartsWith("Language LANG2"));
        Assert.Contains(verdict.FailedChecks, f => f.StartsWith("Unit BOX"));
    }

    [Fact]
    public void Informative_rows_never_sell()
    {
        Assert.False(PriceRowEvaluator.Evaluate(Row("i", 1) with { IsInformative = true }, Context()).Matches);
    }

    [Fact]
    public void Rows_stored_with_vat_are_normalised_before_comparison()
    {
        var withVat = Row("v", 12.1) with { IsWithVat = true }; // 10.00 excl. at 21%
        var without = Row("w", 10.5);

        var result = PriceRowEvaluator.Evaluate([without, withVat], Context());

        Assert.Equal("v", result.Winner!.Row.Id);
        Assert.Equal(10, result.Winner.ComparableAmount, 6);
    }

    [Fact]
    public void Equal_cheapest_rows_flag_a_tie_and_first_wins()
    {
        var result = PriceRowEvaluator.Evaluate([Row("a", 5), Row("b", 5)], Context());

        Assert.True(result.HasTie);
        Assert.Equal("a", result.Winner!.Row.Id);
    }

    [Fact]
    public void No_rows_means_no_winner()
    {
        var result = PriceRowEvaluator.Evaluate([], Context());

        Assert.Null(result.Winner);
        Assert.Equal(0, result.MatchCount);
    }

    [Fact]
    public void Anonymous_context_rejects_every_account_restricted_row()
    {
        var ctx = Context(userId: null) with { UserCustomerNumber = null, UserGroupIds = new HashSet<int>(), UserGroupCustomerNumbers = new HashSet<string>() };

        Assert.False(PriceRowEvaluator.Evaluate(Row("u", 1) with { UserId = "17" }, ctx).Matches);
        Assert.False(PriceRowEvaluator.Evaluate(Row("g", 1) with { UserGroupId = "5" }, ctx).Matches);
        Assert.False(PriceRowEvaluator.Evaluate(Row("c", 1) with { UserCustomerNumber = "C-100" }, ctx).Matches);
        Assert.True(PriceRowEvaluator.Evaluate(Row("open", 1), ctx).Matches);
    }
}
