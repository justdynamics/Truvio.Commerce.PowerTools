using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Models;
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Queries;
using Truvio.Commerce.PowerTools.Shared.AdminUI;
using Truvio.Commerce.PowerTools.Shared.Security;

namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Screens;

/// <summary>Step 1 of the Price Explainer: pick the account to explain prices for.</summary>
public sealed class ExplainerAccountListScreen : ListScreenBase<ExplainerAccountModel>
{
    protected override string GetScreenName() => "Accounts";

#if DW_HAS_SCREEN_EXPLANATION
    protected override string? GetScreenExplanation() =>
        "Pick who is looking; next you pick the product";
#endif

    protected override IEnumerable<ListViewMapping> GetViewMappings() =>
    [
        new RowViewMapping
        {
            Columns =
            [
                CreateMapping(m => m.Kind),
                CreateMapping(m => m.Name),
                CreateMapping(m => m.Detail)
            ]
        }
    ];

    protected override Cell? GetCell(string propertyName, ExplainerAccountModel model) =>
        propertyName == nameof(ExplainerAccountModel.Kind) && !string.IsNullOrEmpty(model.AccountKey)
            ? Badges.AccountKind(model.Kind == "Visitor" ? "Role" : "User")
            : null;

    protected override ActionBase? GetListItemPrimaryAction(ExplainerAccountModel model)
    {
        if (!PowerToolsAccess.CanUsePriceExplainer() || string.IsNullOrEmpty(model.AccountKey))
            return null;

        return NavigateScreenAction.To<ProductPickScreen>()
            .With(new ProductPickQuery { AccountKey = model.AccountKey });
    }
}
