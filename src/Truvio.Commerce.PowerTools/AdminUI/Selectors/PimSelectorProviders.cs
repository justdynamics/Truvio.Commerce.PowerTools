using Dynamicweb.CoreUI;
using Dynamicweb.CoreUI.Actions.Implementations.Components.Selector;
using Dynamicweb.CoreUI.Data;
using Dynamicweb.CoreUI.Data.Filtering;
using Dynamicweb.CoreUI.Editors.Selectors;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Truvio.Commerce.PowerTools.Core.Pim.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Selectors;

/// <summary>One product-group row in the PIM scope picker.</summary>
public sealed class PimGroupPickModel : DataViewModelBase
{
    public string GroupId { get; set; } = string.Empty;

    [ConfigurableProperty("Group", isSearchable: true)]
    public string Name { get; set; } = string.Empty;

    [ConfigurableProperty("Id", isSearchable: true)]
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// The searchable product-group picker behind the PIM toolbar button. A catalog can carry
/// thousands of groups, so this uses DW's slide-over selector with server-side search — the
/// same <c>ShowSearch</c> + <see cref="ISearchable"/> contract the account picker uses.
/// The first row is always "Whole catalog", so a scope can be cleared without a second control.
/// </summary>
public sealed class PimGroupSelectorProvider : SelectorProviderBase<string>, ISearchable
{
    public PimGroupSelectorProvider() : base(1)
    {
    }

    public string Search { get; set; } = string.Empty;

    public override SelectorDefinitionModel GetDefinition() => new()
    {
        ColumnCount = 1,
        Heading = "Select product group"
    };

    protected override UiComponentBase? GetColumnContent(int columnIndex)
    {
        if (columnIndex != 1)
            return null;

        var list = new List();
        list.FillList(
            configuration: new ListScreenConfiguration(GetType()) { EnablePrimaryAction = true, ShowSearch = true },
            listData: Model(),
            viewMappings: Mappings(),
            GetCell: null,
            GetListItemPrimaryAction: model => SelectorItemSelectedAction.Item(new SelectedItem
            {
                Id = model.GroupId,
                Name = model.Name
            }),
            GetListItemContextActions: null,
            GetRowId: model => model.GroupId);
        return list;
    }

    private DataListViewModel<PimGroupPickModel> Model()
    {
        var items = new List<PimGroupPickModel>
        {
            new() { GroupId = PimPicks.WholeCatalog, Name = "Whole catalog", Id = "-" }
        };

        try
        {
            items.AddRange(new DwPimSource().GetGroups()
                .Where(g => string.IsNullOrWhiteSpace(Search) ||
                            g.Name.Contains(Search.Trim(), StringComparison.OrdinalIgnoreCase) ||
                            g.Id.Contains(Search.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(g => new PimGroupPickModel { GroupId = g.Id, Name = g.Name, Id = g.Id }));
        }
        catch
        {
            // A catalog whose groups cannot be read still offers "Whole catalog".
        }

        return new DataListViewModel<PimGroupPickModel> { Data = items, TotalCount = items.Count };
    }

    public override IEnumerable<SelectedItem>? GetSelectedItems(IEnumerable<string> selectedValues) =>
        selectedValues?.Select(value => new SelectedItem { Id = value, Name = value });

    private static IEnumerable<RowViewMapping> Mappings()
    {
        yield return new RowViewMapping
        {
            Columns =
            [
                ModelMapping.CreateFromConfigurableProperty((PimGroupPickModel m) => m.Name),
                ModelMapping.CreateFromConfigurableProperty((PimGroupPickModel m) => m.Id)
            ]
        };
    }
}
