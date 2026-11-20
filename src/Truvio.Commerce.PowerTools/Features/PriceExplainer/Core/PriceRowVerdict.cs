namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

public sealed class PriceRowVerdict
{
    public required PriceRowSpec Row { get; init; }

    /// <summary>True when every DW filter accepts the row for this context.</summary>
    public bool Matches => FailedChecks.Count == 0;

    /// <summary>Human-readable reasons the row was rejected (empty when it matches).</summary>
    public IReadOnlyList<string> FailedChecks { get; init; } = [];

    /// <summary>The restrictions the row carries that the context satisfied ("group 12", "qty >= 10").</summary>
    public IReadOnlyList<string> SatisfiedRestrictions { get; init; } = [];

    /// <summary>Amount DW compares rows by (VAT-normalised); NaN when the row does not match.</summary>
    public double ComparableAmount { get; init; } = double.NaN;

    public bool IsWinner { get; set; }
    public bool IsOutranked { get; set; }
    public bool IsDiscountLine { get; set; }
    public double EffectiveDiscountPercentage { get; set; }
    public double NetAmount => ComparableAmount * (1 - EffectiveDiscountPercentage / 100);
    public bool NeedsHigherQuantity => FailedChecks.Count > 0 && FailedChecks.All(f => f.StartsWith("Needs quantity", StringComparison.Ordinal));
    public string RejectionReason => FailedChecks.Count == 0 ? string.Empty : Reason(FailedChecks.FirstOrDefault(f => !f.StartsWith("Needs quantity", StringComparison.Ordinal)) ?? FailedChecks[0]);

    private static string Reason(string failure) => failure switch
    {
        var f when f.StartsWith("Only for user") || f.StartsWith("Only for customer number") => "Other customers",
        var f when f.StartsWith("Only for") => "Other user groups",
        var f when f.StartsWith("Needs quantity") => "Higher quantity",
        var f when f.StartsWith("Not valid before") => "Not yet valid",
        var f when f.StartsWith("Expired") => "Expired",
        var f when f.StartsWith("Customer price group") => "Other customer price groups",
        var f when f.StartsWith("Customer discount group") => "Other customer discount groups",
        var f when f.StartsWith("Product discount group") => "Other product discount groups",
        var f when f.StartsWith("Invalid") => "Invalid configuration",
        var f when f.StartsWith("DW") => "Rejected by DW",
        _ => failure.Split(' ')[0]
    };

    /// <summary>Set on matching rows that lose to the winner: how much more expensive they are.</summary>
    public double? ShadowedBy { get; set; }
}
