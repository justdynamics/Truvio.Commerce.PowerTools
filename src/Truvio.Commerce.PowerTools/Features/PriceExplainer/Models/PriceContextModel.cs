using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Models;

/// <summary>The context slide-over: currency, shop, quantity and date as click-to-apply lists.</summary>
public sealed class PriceContextModel : DataViewModelBase
{
    public string Heading { get; set; } = "Context";

    public string Html { get; set; } = string.Empty;
}
