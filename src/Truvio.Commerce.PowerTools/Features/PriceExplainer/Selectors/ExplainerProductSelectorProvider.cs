using Dynamicweb.CoreUI;
using Dynamicweb.CoreUI.Actions.Implementations.Components.Selector;
using Dynamicweb.CoreUI.Data;
using Dynamicweb.CoreUI.Data.Filtering;
using Dynamicweb.CoreUI.Editors.Selectors;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Models;
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Queries;

namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Selectors;

/// <summary>
/// The searchable product picker for the Price Explainer's toolbar, over the same
/// <see cref="ProductPickQuery"/> as the full-screen picker (variants included, so a
/// variant-specific price can be explained). The selected id encodes product, variant and
/// language as <c>product~variant~language</c> — <see cref="PriceExplainQuery"/> splits it.
/// </summary>
public sealed class ExplainerProductSelectorProvider : SelectorProviderBase<string>, ISearchable
{
    public const char IdSeparator = '~';

    public ExplainerProductSelectorProvider() : base(1)
    {
    }

    public string Search { get; set; } = string.Empty;

    public override SelectorDefinitionModel GetDefinition() => new()
    {
        ColumnCount = 1,
        Heading = "Select product"
    };

    protected override UiComponentBase? GetColumnContent(int columnIndex)
    {
        if (columnIndex != 1)
            return null;

        var list = new List();
        list.FillList(
            configuration: new ListScreenConfiguration(GetType()) { EnablePrimaryAction = true, ShowSearch = true },
            listData: new ProductPickQuery { Search = Search }.GetModel(),
            viewMappings: Mappings(),
            GetCell: null,
            GetListItemPrimaryAction: model => string.IsNullOrEmpty(model.ProductId)
                ? null
                : SelectorItemSelectedAction.Item(new SelectedItem
                {
                    Id = string.Join(IdSeparator, model.ProductId, model.VariantId, model.LanguageId),
                    Name = model.Name
                }),
            GetListItemContextActions: null,
            GetRowId: model => $"{model.ProductId}/{model.VariantId}");
        return list;
    }

    public override IEnumerable<SelectedItem>? GetSelectedItems(IEnumerable<string> selectedValues) =>
        selectedValues?.Select(value => new SelectedItem
        {
            Id = value,
            Name = value.Split(IdSeparator)[0]
        });

    private static IEnumerable<RowViewMapping> Mappings()
    {
        yield return new RowViewMapping
        {
            Columns =
            [
                ModelMapping.CreateFromConfigurableProperty((ProductPickModel m) => m.Number),
                ModelMapping.CreateFromConfigurableProperty((ProductPickModel m) => m.Name),
                ModelMapping.CreateFromConfigurableProperty((ProductPickModel m) => m.Variant)
            ]
        };
    }
}
