namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

public sealed record PriceStep(double Quantity, string FoundPrice, string FoundDiscount,
    string CampaignDiscounts, string FinalPrice, string Source);
