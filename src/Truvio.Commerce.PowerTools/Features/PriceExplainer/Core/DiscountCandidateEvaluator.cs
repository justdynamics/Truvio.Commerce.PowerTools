namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

/// <summary>
/// Mirrors DiscountProvider.CheckDiscountBaseInfo + the user targeting DW applies when it
/// pre-selects product discounts: currency, shop, validity window, language, country,
/// anonymous flag, and the user / group / customer-number targeting. Product scope and
/// cart-dependent conditions are reported, not decided — DW's own
/// DiscountInfoCollection is the authority on what finally applies.
/// </summary>
public static class DiscountCandidateEvaluator
{
    public static DiscountVerdict Evaluate(DiscountSpec d, DiscountLookupContext ctx)
    {
        var failed = new List<string>();
        var ok = new List<string>();

        if (!d.Active)
            failed.Add("Inactive");

        if (d.ValidFrom is DateTime from && from != DateTime.MinValue)
        {
            if (from <= ctx.Time) ok.Add($"valid from {from:yyyy-MM-dd}");
            else failed.Add($"Not valid before {from:yyyy-MM-dd HH:mm}");
        }

        if (d.ValidTo is DateTime to && to != DateTime.MinValue)
        {
            if (to >= ctx.Time) ok.Add($"valid to {to:yyyy-MM-dd}");
            else failed.Add($"Expired {to:yyyy-MM-dd HH:mm}");
        }

        Restrict(d.CurrencyCode, ctx.CurrencyCode, "currency", failed, ok);
        Restrict(d.ShopId, ctx.ShopId, "shop", failed, ok);
        Restrict(d.LanguageId, ctx.LanguageId, "language", failed, ok);
        Restrict(d.CountryCode, ctx.CountryCode, "country", failed, ok);

        // CheckAnonymous: an "anonymous users" discount applies ONLY to anonymous visitors.
        if (d.AnonymousUsers)
        {
            if (ctx.UserId is null) ok.Add("anonymous users");
            else failed.Add("Only for anonymous users (the selected account is signed in)");
        }

        // User targeting (DiscountService pre-selection keys: user id, group id, customer number).
        if (d.UserId is int uid && uid > 0)
        {
            if (ctx.UserId == uid) ok.Add($"user {uid}");
            else failed.Add($"Only for user id {uid}");
        }

        if (d.UserGroupId is int gid && gid > 0)
        {
            if (ctx.UserGroupIds.Contains(gid)) ok.Add($"group {gid}");
            else failed.Add($"Only for members of group {gid}");
        }

        if (!string.IsNullOrWhiteSpace(d.UserCustomerNumber))
        {
            if (string.Equals(d.UserCustomerNumber, ctx.UserCustomerNumber, StringComparison.OrdinalIgnoreCase))
                ok.Add($"customer no. {d.UserCustomerNumber}");
            else
                failed.Add($"Only for customer number {d.UserCustomerNumber}");
        }

        if (d.NeedsOrder)
            failed.Add($"Needs a cart to evaluate: {d.NeedsOrderReason}");

        return new DiscountVerdict
        {
            Discount = d,
            FailedChecks = failed,
            SatisfiedRestrictions = ok
        };
    }

    private static void Restrict(string required, string? actual, string what, List<string> failed, List<string> ok)
    {
        if (string.IsNullOrEmpty(required))
            return;
        if (string.Equals(required, actual ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            ok.Add($"{what} {required}");
        else
            failed.Add($"{Capitalize(what)} {required} ≠ {(string.IsNullOrEmpty(actual) ? "none" : actual)}");
    }

    private static string Capitalize(string s) => char.ToUpperInvariant(s[0]) + s[1..];
}
