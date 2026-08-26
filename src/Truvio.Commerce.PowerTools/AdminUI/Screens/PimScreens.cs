using Dynamicweb.CoreUI;
using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Displays.Widgets;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Truvio.Commerce.PowerTools.AdminUI.Security;
using Truvio.Commerce.PowerTools.Core.Pim;
using Truvio.Commerce.PowerTools.Core.Pim.Dw;
using Truvio.Commerce.PowerTools.Core.Settings;
using Truvio.Commerce.PowerTools.Core.Settings.Dw;
using Icon = Dynamicweb.CoreUI.Icons.Icon;

namespace Truvio.Commerce.PowerTools.AdminUI.Screens;

/// <summary>
/// Screen 1 — the ranked, scoped list of incomplete products. Family rows: DW scores a family,
/// and a catalog with variants would otherwise render tens of thousands of rows. The drill-down
/// unfolds the variants.
/// </summary>
public sealed class PimCompletenessScreen : ListScreenBase<PimCompletenessModel>
{
    private PimCompletenessQuery Q => Query as PimCompletenessQuery ?? new PimCompletenessQuery();

    protected override string GetScreenName() => "Completeness explorer";

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
            var shopId = Core.Commerce.Dw.DwPdpLocator.ShopForGroup(Q.GroupId, Q.LanguageId);
            return Core.Commerce.Dw.DwPdpLocator.UrlFor(shopId, Q.ProductId);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Screen 3 — the section landing: how healthy the catalog is, every PIM finding, and the
/// "fix this first" field ranking that turns a score into a work order.
/// </summary>
public sealed class PimQualityScreen : OverviewScreenBase<PimQualityModel>
{
    private PimQualityQuery Q => Query as PimQualityQuery ?? new PimQualityQuery();

    protected override string GetScreenName() => "Catalog quality";

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

/// <summary>
/// Screen 4 — governance: which completion rules govern anything, and which workflows the
/// catalog references. Stuck-state ageing is deliberately absent — DW exposes no read API for
/// how long a product has sat in a workflow state.
/// </summary>
public sealed class PimGovernanceScreen : ListScreenBase<PimGovernanceModel>
{
    protected override string GetScreenName() => "Rules & workflows";

#if DW_HAS_SCREEN_EXPLANATION
    protected override string? GetScreenExplanation() =>
        "Completion rules with what they are assigned to, and the workflows product groups and products reference — 'Dead' means the rule scores nothing";
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
                    NodeAction = NavigateScreenAction.To<PimQualityScreen>().With(new PimQualityQuery())
                },
                new ActionNode
                {
                    Name = "Completeness explorer",
                    Icon = Icon.Tag,
                    NodeAction = NavigateScreenAction.To<PimCompletenessScreen>().With(new PimCompletenessQuery())
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
                CreateMapping(m => m.Status),
                CreateMapping(m => m.Kind),
                CreateMapping(m => m.Name),
                CreateMapping(m => m.AppliesTo),
                CreateMapping(m => m.Fields)
            ]
        }
    ];

    protected override Cell? GetCell(string propertyName, PimGovernanceModel model) =>
        propertyName == nameof(PimGovernanceModel.Status)
            ? PimBadges.State(model.State, model.Status)
            : null;
}

/// <summary>
/// The scope switches for the PIM screens, in the top bar next to Actions: a searchable group
/// picker (a catalog can hold thousands of groups) and a language switch. An injector because
/// the screen bases keep their <c>ScreenLayout</c> private.
/// </summary>
public sealed class PimCompletenessToolbarInjector : ScreenInjector<PimCompletenessScreen>
{
    public override void OnAfter(PimCompletenessScreen screen, UiComponentBase content)
    {
        if (content is not ScreenLayout layout)
            return;

        var q = screen.Query as PimCompletenessQuery ?? new PimCompletenessQuery();

        PimToolbar.AddGroupPicker(layout, q.GroupId, token =>
            NavigateScreenAction.To<PimCompletenessScreen>()
                .With(new PimCompletenessQuery { LanguageId = q.LanguageId, GroupPickToken = token }));

        PimToolbar.AddLanguageSwitch(layout, q.LanguageId, languageId =>
            NavigateScreenAction.To<PimCompletenessScreen>()
                .With(new PimCompletenessQuery { GroupId = q.GroupId, LanguageId = languageId }));
    }
}

/// <summary>The same scope switches on the catalog overview.</summary>
public sealed class PimQualityToolbarInjector : ScreenInjector<PimQualityScreen>
{
    public override void OnAfter(PimQualityScreen screen, UiComponentBase content)
    {
        if (content is not ScreenLayout layout)
            return;

        var q = screen.Query as PimQualityQuery ?? new PimQualityQuery();

        PimToolbar.AddGroupPicker(layout, q.GroupId, token =>
            NavigateScreenAction.To<PimQualityScreen>()
                .With(new PimQualityQuery { LanguageId = q.LanguageId, GroupPickToken = token }));

        PimToolbar.AddLanguageSwitch(layout, q.LanguageId, languageId =>
            NavigateScreenAction.To<PimQualityScreen>()
                .With(new PimQualityQuery { GroupId = q.GroupId, LanguageId = languageId }));
    }
}

/// <summary>Shared toolbar wiring so both PIM screens offer identical scope controls.</summary>
internal static class PimToolbar
{
    public static void AddGroupPicker(ScreenLayout layout, string groupId, Func<string, NavigateScreenAction> onPicked)
    {
        var token = Guid.NewGuid().ToString("N");
        ToolbarSwitch.AddPicker(layout, Label(groupId), Icon.Sitemap,
            new Selectors.PimGroupSelectorProvider(), token, onPicked(token));
    }

    public static void AddLanguageSwitch(ScreenLayout layout, string languageId, Func<string, NavigateScreenAction> onPicked)
    {
        var languages = Safe(() => new DwPimSource().GetLanguages());
        if (languages.Count <= 1)
            return;

        var options = languages
            .Select(l => ToolbarSwitch.Option(
                $"{l.Name} ({l.Id})",
                active: string.Equals(l.Id, languageId, StringComparison.OrdinalIgnoreCase) ||
                        (string.IsNullOrEmpty(languageId) && l.Id == languages[0].Id),
                onPicked(l.Id)))
            .ToList();

        var current = languages.FirstOrDefault(l => string.Equals(l.Id, languageId, StringComparison.OrdinalIgnoreCase));
        ToolbarSwitch.Add(layout, string.IsNullOrEmpty(current.Id) ? "Language" : current.Id, Icon.Globe, options);
    }

    private static string Label(string groupId)
    {
        if (string.IsNullOrEmpty(groupId))
            return "Whole catalog";

        var group = Safe(() => new DwPimSource().GetGroups())
            .FirstOrDefault(g => string.Equals(g.Id, groupId, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrEmpty(group.Name) ? groupId : group.Name;
    }

    private static IReadOnlyList<T> Safe<T>(Func<IReadOnlyList<T>> source)
    {
        try
        {
            return source();
        }
        catch
        {
            return [];
        }
    }
}
