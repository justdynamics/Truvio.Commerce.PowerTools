namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

public sealed class DiscountVerdict
{
    public required DiscountSpec Discount { get; init; }
    /// <summary>Reasons the discount cannot apply in this context (empty = base conditions pass).</summary>
    public IReadOnlyList<string> FailedChecks { get; init; } = [];
    public IReadOnlyList<string> SatisfiedRestrictions { get; init; } = [];
    public string RejectionReason => FailedChecks.FirstOrDefault() switch
    {
        null => string.Empty,
        var f when f.StartsWith("Only for anonymous") => "Anonymous-only discounts",
        var f when f.StartsWith("Only for members") => "Other user groups",
        var f when f.StartsWith("Only for") => "Other customers",
        var f when f.StartsWith("Not valid before") => "Not yet valid",
        var f when f.StartsWith("Needs a cart") => "Needs a cart",
        var f => f.Split(' ')[0]
    };
    public bool PassesBaseChecks => FailedChecks.Count == 0;
}
