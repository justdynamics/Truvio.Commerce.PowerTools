using Dynamicweb.CoreUI;
using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Displays.Widgets;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Icon = Dynamicweb.CoreUI.Icons.Icon;
using Truvio.Commerce.PowerTools.Features.PimQuality.Models;
using Truvio.Commerce.PowerTools.Features.PimQuality.Queries;
using Truvio.Commerce.PowerTools.Features.Settings.Queries;
using Truvio.Commerce.PowerTools.Features.Settings.Screens;
using Truvio.Commerce.PowerTools.Shared.AdminUI;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Screens;

/// <summary>
/// Screen 3 — the section landing: how healthy the catalog is, every PIM finding, and the
/// "fix this first" field ranking that turns a score into a work order.
/// </summary>
public sealed class PimQualityScreen : OverviewScreenBase<PimQualityModel>
{
    private PimQualityQuery Q => Query as PimQualityQuery ?? new PimQualityQuery();

    protected override string GetScreenName() => "Overview";

    protected override void BuildOverviewScreen()
    {
        var model = Model;
        if (model is null)
            return;

        if (!string.IsNullOrEmpty(model.Error))
        {
            AddComponent(new Alert { Value = model.Error, Icon = Icon.ExclamationTriangle }, "Quality check failed", Group.GroupWidth.Col_12);
            return;
        }

        SetInfobar(new InfoBar
        {
            Icon = Icon.Heartbeat,
            Information = new Dictionary<string, CardInfo.InfoValue>
            {
                ["Catalog"] = new(new Badge
                {
                    Value = model.Verdict,
                    BadgeType = model.Healthy ? BadgeType.Success : BadgeType.Warning
                }),
                ["Products scanned"] = new(model.ProductsScanned),
                ["Average completeness"] = new(model.AverageScore),
                ["Incomplete"] = new(model.BelowThreshold),
                ["Fix first"] = new(model.WorstField),
                ["Variant gaps"] = new(model.VariantGaps),
                ["Broken images"] = new(model.BrokenImages),
                ["Dead rules"] = new(model.DeadRules),
                ["Findings"] = new(model.FindingCounts)
            }
        });

        if (!string.IsNullOrEmpty(model.ScopeNote))
            AddComponent(new Alert { Value = model.ScopeNote, Icon = Icon.InfoCircle }, "Scope", Group.GroupWidth.Col_12);

        AddComponent(new HtmlBlock { Value = OpsHtml.Table(model.WorstFields) }, "Fix these fields first", Group.GroupWidth.Col_12);
        AddComponent(new HtmlBlock { Value = OpsHtml.Table(model.Findings) }, "Findings", Group.GroupWidth.Col_12);
    }

    protected override IEnumerable<ActionGroup>? GetScreenActions() =>
    [
        new()
        {
            Nodes =
            [
                new ActionNode
                {
                    Name = "Completeness explorer",
                    Icon = Icon.Tag,
                    NodeAction = NavigateScreenAction.To<PimCompletenessScreen>()
                        .With(new PimCompletenessQuery { GroupId = Q.GroupId, LanguageId = Q.LanguageId })
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
}
