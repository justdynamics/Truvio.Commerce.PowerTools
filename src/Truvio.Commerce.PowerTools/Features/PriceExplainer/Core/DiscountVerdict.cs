namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

public sealed class DiscountVerdict
{
    public required DiscountSpec Discount { get; init; }
    /// <summary>Reasons the discount cannot apply in this context (empty = base conditions pass).</summary>
    public IReadOnlyList<string> FailedChecks { get; init; } = [];
    public IReadOnlyList<string> SatisfiedRestrictions { get; init; } = [];
    public bool PassesBaseChecks => FailedChecks.Count == 0;
}
