using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.AdminUI.Models;

/// <summary>One product (or variant) row in the product picker.</summary>
public sealed class ProductPickModel : DataViewModelBase
{
    public string ProductId { get; set; } = string.Empty;

    public string VariantId { get; set; } = string.Empty;

    public string LanguageId { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    [ConfigurableProperty("Number", isSearchable: true)]
    public string Number { get; set; } = string.Empty;

    [ConfigurableProperty("Name", isSearchable: true)]
    public string Name { get; set; } = string.Empty;

    [ConfigurableProperty("Variant", isSearchable: true)]
    public string Variant { get; set; } = string.Empty;

    [ConfigurableProperty("Active")]
    public string Active { get; set; } = string.Empty;

    [ConfigurableProperty("Default price")]
    public string DefaultPrice { get; set; } = string.Empty;
}
