using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Models;

/// <summary>The Price Explainer report as one overview model: a headline plus sectioned rows.</summary>
public sealed class PriceExplainModel : DataViewModelBase
{
    public string Title { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public bool Visible { get; set; }

    public IReadOnlyList<Core.PriceStep> Steps { get; set; } = [];

    public string Error { get; set; } = string.Empty;

    public List<ExplainRowModel> Rows { get; set; } = [];
}
