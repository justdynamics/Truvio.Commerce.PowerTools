namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

public sealed class PriceMatrixVerdict
{
    public IReadOnlyList<PriceRowVerdict> Rows { get; init; } = [];

    public PriceRowVerdict? Winner { get; init; }

    /// <summary>True when two or more matching rows share the lowest amount: DW picks whichever the DB returns first.</summary>
    public bool HasTie { get; init; }

    public int MatchCount => Rows.Count(r => r.Matches);
}
