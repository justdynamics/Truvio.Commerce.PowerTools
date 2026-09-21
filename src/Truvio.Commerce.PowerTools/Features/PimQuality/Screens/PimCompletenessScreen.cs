using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Icon = Dynamicweb.CoreUI.Icons.Icon;
using Truvio.Commerce.PowerTools.Features.PimQuality.Models;
using Truvio.Commerce.PowerTools.Features.PimQuality.Queries;
using Truvio.Commerce.PowerTools.Features.Settings.Queries;
using Truvio.Commerce.PowerTools.Features.Settings.Screens;
using Truvio.Commerce.PowerTools.Shared.Security;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Screens;

/// <summary>
/// Screen 1 — the ranked, scoped list of incomplete products. Family rows: DW scores a family,
/// and a catalog with variants would otherwise render tens of thousands of rows. The drill-down
/// unfolds the variants.
/// </summary>
public sealed class PimCompletenessScreen : ListScreenBase<PimCompletenessModel>
{
    private PimCompletenessQuery Q => Query as PimCompletenessQuery ?? new PimCompletenessQuery();

    protected override string GetScreenName() => "Overview";

#if DW_HAS_SCREEN_EXPLANATION
    protected override string? GetScreenExplanation() =>
        "DW's own completeness score for every product family in scope, worst first — pick a row to see which field is missing where";
#endif

    protected override IEnumerable<ActionGroup>? GetScreenActions() =>
    [
        new()
        {
            Nodes =
            [
                new ActionNode
                {
                    Name = "Catalog quality",
                    Icon = Icon.Heartbeat,
                    NodeAction = NavigateScreenAction.To<PimQualityScreen>()
                        .With(new PimQualityQuery { GroupId = Q.GroupId, LanguageId = Q.LanguageId })
                },
                new ActionNode
                {
                    Name = "Rules & workflows",
                    Icon = Icon.Sitemap,
                    NodeAction = NavigateScreenAction.To<PimGovernanceScreen>().With(new PimGovernanceQuery())
                },
                new ActionNode
                {
                    Name = "PowerTools settings",
                    Icon = Icon.Cog,
                    NodeAction = NavigateScreenAction.To<PowerToolsSettingsScreen>().With(new PowerToolsSettingsQuery())
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
                CreateMapping(m => m.Score),
                CreateMapping(m => m.Number),
                CreateMapping(m => m.Name),
                CreateMapping(m => m.WorstRule),
                CreateMapping(m => m.MissingCount),
                CreateMapping(m => m.MissingFields)
            ]
        }
    ];

    // The trailing "N more products" row has no score: no badge for it.
    protected override Cell? GetCell(string propertyName, PimCompletenessModel model) =>
        propertyName == nameof(PimCompletenessModel.Score) && !string.IsNullOrEmpty(model.Score)
            ? PimBadges.Score(model.ScoreValue, model.Score)
            : null;

    protected override ActionBase? GetListItemPrimaryAction(PimCompletenessModel model) =>
        PowerToolsAccess.CanUsePim() && !string.IsNullOrEmpty(model.ProductId)
            ? NavigateScreenAction.To<PimProductQualityScreen>()
                .With(new PimProductQualityQuery
                {
                    ProductId = model.ProductId,
                    LanguageId = model.LanguageId,
                    GroupId = Q.GroupId
                })
            : null;
}
