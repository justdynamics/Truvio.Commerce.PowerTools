namespace Truvio.Commerce.PowerTools.Core.Commerce;

/// <summary>An order-line (product) discount, reduced to the conditions DW checks before applying it to a product price.</summary>
public sealed record DiscountSpec
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool Active { get; init; }
    public int Priority { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public string ShopId { get; init; } = string.Empty;
    public string LanguageId { get; init; } = string.Empty;
    public bool AnonymousUsers { get; init; }
    public int? UserId { get; init; }
    public int? UserGroupId { get; init; }
    public string UserCustomerNumber { get; init; } = string.Empty;
    /// <summary>Free-text description of the discount's product scope ("all products", "2 products, 1 group", ...).</summary>
    public string ProductScope { get; init; } = string.Empty;
    /// <summary>True when the discount needs an order to be evaluated (order total, order field, product quantity &gt; 1, voucher).</summary>
    public bool NeedsOrder { get; init; }
    public string NeedsOrderReason { get; init; } = string.Empty;
    public string TypeDescription { get; init; } = string.Empty;
    public bool StopFurtherProcessing { get; init; }
    public bool OnlyApplyToNonDiscountedItems { get; init; }
}

public sealed record DiscountLookupContext
{
    public int? UserId { get; init; }
    public string? UserCustomerNumber { get; init; }
    public IReadOnlySet<int> UserGroupIds { get; init; } = new HashSet<int>();
    public string CurrencyCode { get; init; } = string.Empty;
    public string? CountryCode { get; init; }
    public string? ShopId { get; init; }
    public string LanguageId { get; init; } = string.Empty;
    public DateTime Time { get; init; } = DateTime.Now;
}

public sealed class DiscountVerdict
{
    public required DiscountSpec Discount { get; init; }
    /// <summary>Reasons the discount cannot apply in this context (empty = base conditions pass).</summary>
    public IReadOnlyList<string> FailedChecks { get; init; } = [];
    public IReadOnlyList<string> SatisfiedRestrictions { get; init; } = [];
    public bool PassesBaseChecks => FailedChecks.Count == 0;
}

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
