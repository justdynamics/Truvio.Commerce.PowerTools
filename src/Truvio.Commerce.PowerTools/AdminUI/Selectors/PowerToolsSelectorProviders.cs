using Dynamicweb.CoreUI;
using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations.Components.Selector;
using Dynamicweb.CoreUI.Data;
using Dynamicweb.CoreUI.Data.Filtering;
using Dynamicweb.CoreUI.Editors.Selectors;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Truvio.Commerce.PowerTools.Core.Principals;
using Truvio.Commerce.PowerTools.Core.Principals.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Selectors;

/// <summary>
/// The searchable account picker behind the toolbar buttons: DW's slide-over selector
/// (<c>OpenSlideOverAction</c> → <c>SelectorScreen</c>) asks the provider for one column and
/// pushes the panel's search text into <see cref="ISearchable.Search"/> before every render,
/// so the filtering happens server-side and an install with thousands of users stays usable.
/// Rows come from the same <see cref="AccountListQuery"/> the full-screen picker uses.
/// </summary>
public sealed class AccountSelectorProvider : SelectorProviderBase<string>, ISearchable
{
    public AccountSelectorProvider() : base(1)
    {
    }

    public string Search { get; set; } = string.Empty;

    public override SelectorDefinitionModel GetDefinition() => new()
    {
        ColumnCount = 1,
        Heading = "Select account"
    };

    protected override UiComponentBase? GetColumnContent(int columnIndex)
    {
        if (columnIndex != 1)
            return null;

        var list = new List();
        list.FillList(
            configuration: new ListScreenConfiguration(GetType()) { EnablePrimaryAction = true, ShowSearch = true },
            listData: new AccountListQuery { Search = Search }.GetModel(),
            viewMappings: Mappings(),
            GetCell: null,
            GetListItemPrimaryAction: model => string.IsNullOrEmpty(model.AccountKey)
                ? null
                : SelectorItemSelectedAction.Item(Item(model.AccountKey, model.Name, model.Kind)),
            GetListItemContextActions: null,
            GetRowId: model => model.AccountKey);
        return list;
    }

    public override IEnumerable<SelectedItem>? GetSelectedItems(IEnumerable<string> selectedValues) =>
        selectedValues?.Select(key =>
        {
            var account = Resolve(key);
            return Item(key, account?.DisplayName ?? key, account?.Kind.ToString() ?? string.Empty);
        });

    private static SecurityAccount? Resolve(string key)
    {
        try
        {
            return new DwAccountCatalog().Resolve(key);
        }
        catch
        {
            return null;
        }
    }

    private static SelectedItem Item(string key, string name, string kind) => new()
    {
        Id = key,
        Name = name,
        Group = kind
    };

    private static IEnumerable<RowViewMapping> Mappings()
    {
        yield return new RowViewMapping
        {
            Columns =
            [
                ModelMapping.CreateFromConfigurableProperty((AccountListModel m) => m.Kind),
                ModelMapping.CreateFromConfigurableProperty((AccountListModel m) => m.Name),
                ModelMapping.CreateFromConfigurableProperty((AccountListModel m) => m.Detail)
            ]
        };
    }
}

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
