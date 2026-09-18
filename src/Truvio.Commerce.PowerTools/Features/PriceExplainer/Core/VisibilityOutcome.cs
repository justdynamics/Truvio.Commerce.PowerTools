namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

public enum VisibilityOutcome
{
    /// <summary>Assortments are switched off or none is active: every product is visible to everyone.</summary>
    AssortmentsInactive,
    /// <summary>The product is in no assortment at all — visible on direct access, but hidden from assortment-filtered lists.</summary>
    InNoAssortment,
    /// <summary>The account holds at least one assortment containing the product.</summary>
    Visible,
    /// <summary>The product sits in assortments the account does not hold.</summary>
    Hidden
}
