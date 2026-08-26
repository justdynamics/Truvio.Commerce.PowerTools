using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.AdminUI.Models;

/// <summary>The Price Explainer report as one overview model: a headline plus sectioned rows.</summary>
public sealed class PriceExplainModel : DataViewModelBase
{
    public string Title { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    /// <summary>"Yes"/"No" plus the summary sentence.</summary>
    public bool Visible { get; set; }

    public string VisibilitySummary { get; set; } = string.Empty;

    public string PriceBeforeDiscount { get; set; } = string.Empty;

    public string PriceSource { get; set; } = string.Empty;

    public string DiscountTotal { get; set; } = string.Empty;

    public int AppliedDiscountCount { get; set; }

    public string FinalPrice { get; set; } = string.Empty;

    public string Error { get; set; } = string.Empty;

    public List<ExplainRowModel> Rows { get; set; } = [];
}

/// <summary>The context slide-over: currency, shop, quantity and date as click-to-apply lists.</summary>
public sealed class PriceContextModel : DataViewModelBase
{
    public string Heading { get; set; } = "Context";

    public string Html { get; set; } = string.Empty;
}
