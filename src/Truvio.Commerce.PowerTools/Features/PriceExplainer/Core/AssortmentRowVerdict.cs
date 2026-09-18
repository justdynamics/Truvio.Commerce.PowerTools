namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

public sealed class AssortmentRowVerdict
{
    public required AssortmentSpec Assortment { get; init; }
    /// <summary>The account is entitled to this assortment (permission or anonymous flag), and it is active.</summary>
    public bool AccountHasIt { get; init; }
    public string Explanation { get; init; } = string.Empty;
    /// <summary>True when this assortment is what makes the product visible to the account.</summary>
    public bool Grants { get; init; }
}
