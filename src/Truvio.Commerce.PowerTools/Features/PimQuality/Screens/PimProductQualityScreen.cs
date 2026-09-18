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
using Truvio.Commerce.PowerTools.Features.PimQuality.Core;
using Truvio.Commerce.PowerTools.Features.PimQuality.Models;
using Truvio.Commerce.PowerTools.Features.PimQuality.Queries;
using Truvio.Commerce.PowerTools.Features.ProductPreview.Dw;
using Truvio.Commerce.PowerTools.Features.Settings.Core;
using Truvio.Commerce.PowerTools.Features.Settings.Dw;
using Truvio.Commerce.PowerTools.Shared.AdminUI;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Screens;

/// <summary>
/// Screen 2 — the "why" for one product: which field is missing, under which rule, in which
/// language, on which variant. An overview screen, not a list: the list grid gives every column
/// the same width and clips long text, and these explanations are long text.
/// </summary>
public sealed class PimProductQualityScreen : OverviewScreenBase<PimProductQualityModel>
{
    private PimProductQualityQuery Q => Query as PimProductQualityQuery ?? new PimProductQualityQuery();

    protected override string GetScreenName() =>
        Model is null || string.IsNullOrEmpty(Model.Title) ? "Product quality" : $"Product quality: {Model.Title}";

    protected override void BuildOverviewScreen()
    {
        var model = Model;
        if (model is null)
            return;

        if (!string.IsNullOrEmpty(model.Error))
        {
            AddComponent(new Alert { Value = model.Error, Icon = Icon.ExclamationTriangle }, "Report failed", Group.GroupWidth.Col_12);
            return;
        }

        var threshold = PowerToolsSettings.Positive(
            DwPowerToolsSettings.Current.PimCompletenessThreshold, PimQualityEngine.DefaultThreshold);

        SetInfobar(new InfoBar
        {
            Icon = Icon.Tag,
            Information = new Dictionary<string, CardInfo.InfoValue>
            {
                ["Product"] = new(model.ProductName),
                ["Language"] = new(model.LanguageId),
                ["Complete"] = new(new Badge
                {
                    Value = model.Score,
                    BadgeType = model.ScoreValue >= threshold ? BadgeType.Success : BadgeType.Warning
                }),
                ["Rules applied"] = new(model.RulesApplied),
                ["Fields missing"] = new(model.MissingCount),
                ["Languages behind"] = new(model.LanguagesBehind)
            }
        });

        foreach (var section in model.Sections)
            AddComponent(new HtmlBlock { Value = OpsHtml.Table(section.Rows) }, section.Heading, Group.GroupWidth.Col_12);
    }

    protected override IEnumerable<ActionGroup>? GetScreenActions()
    {
        var nodes = new List<ActionNode>
        {
            new()
            {
                Name = "Back to the explorer",
                Icon = Icon.Tag,
                NodeAction = NavigateScreenAction.To<PimCompletenessScreen>()
                    .With(new PimCompletenessQuery { GroupId = Q.GroupId, LanguageId = Q.LanguageId })
            },
            new()
            {
                Name = "Catalog quality",
                Icon = Icon.Heartbeat,
                NodeAction = NavigateScreenAction.To<PimQualityScreen>().With(new PimQualityQuery())
            }
        };

        // The storefront PDP for this product, in a new tab — mapped in PowerTools settings
        // or auto-detected from the shop's website.
        var previewUrl = SafePreviewUrl();
        if (previewUrl is not null)
            nodes.Add(new ActionNode
            {
                Name = "Preview in shop",
                Icon = Icon.ExternalLinkAlt,
                NodeAction = NavigateLinkAction.To(previewUrl)
            });

        return [new ActionGroup { Nodes = nodes }];
    }

    private string? SafePreviewUrl()
    {
        try
        {
            var shopId = DwPdpLocator.ShopForGroup(Q.GroupId, Q.LanguageId);
            return DwPdpLocator.UrlFor(shopId, Q.ProductId);
        }
        catch
        {
            return null;
        }
    }
}
