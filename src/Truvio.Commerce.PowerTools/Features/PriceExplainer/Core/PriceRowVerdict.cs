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

    /// <summary>Set on matching rows that lose to the winner: how much more expensive they are.</summary>
    public double? ShadowedBy { get; set; }
}
