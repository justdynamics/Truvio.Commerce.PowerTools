namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

public sealed class PriceMatrixVerdict
{
    public IReadOnlyList<PriceRowVerdict> Rows { get; init; } = [];

    public PriceRowVerdict? Winner { get; init; }

    /// <summary>True when two or more matching rows share the lowest net amount in the winning priority tier: DW picks whichever the DB returns first.</summary>
    public bool HasTie { get; init; }

    public int MatchCount => Rows.Count(r => r.Matches);
    public PriceSelectionRule Rule { get; init; }
    public double FallbackDiscountPercentage { get; init; }
    public IReadOnlyDictionary<string, int> RejectedCounts => Rows.Where(r => !r.Matches)
        .GroupBy(r => r.RejectionReason).ToDictionary(g => g.Key, g => g.Count());
}
