using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Icons;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Truvio.Commerce.PowerTools.AdminUI.Security;

namespace Truvio.Commerce.PowerTools.AdminUI.Screens;

/// <summary>Step 2 of the Price Explainer: pick the product (or variant).</summary>
public sealed class ProductPickScreen : ListScreenBase<ProductPickModel>
{
    private string AccountKey => (Query as ProductPickQuery)?.AccountKey ?? string.Empty;

    protected override string GetScreenName() => "Price Explainer - products";

#if DW_HAS_SCREEN_EXPLANATION
    protected override string? GetScreenExplanation() =>
        "Search by number, name or id; pick a variant row to explain that variant's price";
#endif

    protected override IEnumerable<ActionGroup>? GetScreenActions() =>
    [
        new()
        {
            Nodes =
            [
                new ActionNode
                {
                    Name = "Select another account",
                    Icon = Icon.UserCircle,
                    NodeAction = NavigateScreenAction.To<ExplainerAccountListScreen>()
                        .With(new ExplainerAccountListQuery())
                }
            ]
        }
    ];

    protected override IEnumerable<ListViewMapping> GetViewMappings() =>
    [
        new RowViewMapping
        {
            Columns =
            [
                CreateMapping(m => m.Number),
                CreateMapping(m => m.Name),
                CreateMapping(m => m.Variant),
                CreateMapping(m => m.Active),
                CreateMapping(m => m.DefaultPrice)
            ]
        }
    ];

    protected override Cell? GetCell(string propertyName, ProductPickModel model) =>
        propertyName == nameof(ProductPickModel.Active) && !string.IsNullOrEmpty(model.ProductId)
            ? Badges.Visible(model.IsActive, model.Active)
            : null;

    protected override ActionBase? GetListItemPrimaryAction(ProductPickModel model)
    {
        if (!PowerToolsAccess.CanUsePriceExplainer() || string.IsNullOrEmpty(model.ProductId))
            return null;

        return NavigateScreenAction.To<PriceExplainScreen>()
            .With(new PriceExplainQuery
            {
                AccountKey = AccountKey,
                ProductId = model.ProductId,
                VariantId = model.VariantId,
                LanguageId = model.LanguageId
            });
    }
}
