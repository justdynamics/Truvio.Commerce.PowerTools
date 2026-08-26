using Dynamicweb.CoreUI;
using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Displays.Widgets;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Icon = Dynamicweb.CoreUI.Icons.Icon;

namespace Truvio.Commerce.PowerTools.AdminUI.Screens;

/// <summary>
/// What stands out about one account's content experience — and how it differs from another
/// account's. The question it answers is the one asked after the permissions are configured:
/// "the Lumber role should see its dashboard and the Roofing role theirs — did that land?"
/// <para>
/// It reports ACCESS, not rendering: no page is fetched or previewed, so a template that hides
/// content for its own reasons is out of scope. Overview screen, not a list — every row carries
/// an explanation, and the list grid clips long text.
/// </para>
/// </summary>
public sealed class ExperienceAnalyzerScreen : OverviewScreenBase<ExperienceAnalyzerModel>
{
    protected override string GetScreenName() =>
        Model is null || string.IsNullOrEmpty(Model.Title) ? "Experience Analyzer" : Model.Title;

    protected override void BuildOverviewScreen()
    {
        var model = Model;
        if (model is null)
            return;

        if (!string.IsNullOrEmpty(model.Error))
        {
            AddComponent(new Alert { Value = model.Error, Icon = Icon.ExclamationTriangle }, "Analysis failed", Group.GroupWidth.Col_12);
            return;
        }

        var information = new Dictionary<string, CardInfo.InfoValue>
        {
            ["Account"] = new(model.AccountName)
        };

        if (model.Comparing)
            information["Compared with"] = new(model.CompareName);

        information["Website"] = new(model.Scope);
        information["Sees"] = new(model.VisibleA);

        if (!string.IsNullOrEmpty(model.VisibleB))
            information[model.Comparing ? "They see" : "Public sees"] = new(model.VisibleB);

        information["Differences"] = new(new Badge
        {
            Value = model.Identical ? "None" : model.DifferenceCount.ToString(),
            BadgeType = model.Identical ? BadgeType.Success : BadgeType.Warning
        });

        SetInfobar(new InfoBar { Icon = Icon.Balance, Information = information });

        foreach (var section in model.Sections)
            AddComponent(new HtmlBlock { Value = section.Html }, section.Heading, Group.GroupWidth.Col_12);
    }

    protected override IEnumerable<ActionGroup>? GetScreenActions()
    {
        var q = Query as ExperienceAnalyzerQuery ?? new ExperienceAnalyzerQuery();
        var (accountKey, _) = q.EffectiveKeys();

        return
        [
            new ActionGroup
            {
                Nodes =
                [
                    new ActionNode
                    {
                        Name = "Open in Content Access Viewer",
                        Icon = Icon.Shield,
                        NodeAction = NavigateScreenAction.To<AccessOverviewScreen>()
                            .With(new AccessOverviewQuery { AccountKey = accountKey, AreaId = q.AreaId })
                    }
                ]
            }
        ];
    }
}

/// <summary>
/// The two account pickers and the website switch. Two pickers means two distinct pick tokens:
/// one store entry per dimension, or the second pick would overwrite the first.
/// </summary>
public sealed class ExperienceAnalyzerToolbarInjector : ScreenInjector<ExperienceAnalyzerScreen>
{
    public override void OnAfter(ExperienceAnalyzerScreen screen, UiComponentBase content)
    {
        if (content is not ScreenLayout layout)
            return;

        if (screen.Query is not ExperienceAnalyzerQuery q)
            return;

        var (accountKey, compareKey) = q.EffectiveKeys();
        if (string.IsNullOrEmpty(accountKey))
            return;

        var accountToken = Guid.NewGuid().ToString("N");
        ToolbarSwitch.AddPicker(layout, Resolve(accountKey), Icon.UserCircle,
            new Selectors.AccountSelectorProvider(), accountToken,
            NavigateScreenAction.To<ExperienceAnalyzerScreen>()
                .With(new ExperienceAnalyzerQuery { PickToken = accountToken, CompareKey = compareKey, AreaId = q.AreaId }));

        var compareToken = Guid.NewGuid().ToString("N");
        var pickCompare = OpenSlideOverAction
            .To<Dynamicweb.Application.UI.Screens.SelectorScreen>()
            .With(new Dynamicweb.Application.UI.Queries.SelectorDataByProviderQuery(new Selectors.AccountSelectorProvider()))
            .WithOnSelectAction(
                RunCommandAction
                    .For(new Commands.ToolbarPickCommand { Token = compareToken })
                    .WithCommandProperty(nameof(Commands.ToolbarPickCommand.PickedId))
                    .WithOnSuccess(NavigateScreenAction.To<ExperienceAnalyzerScreen>()
                        .With(new ExperienceAnalyzerQuery { AccountKey = accountKey, ComparePickToken = compareToken, AreaId = q.AreaId })
                        .WithForceReload()));

        if (string.IsNullOrEmpty(compareKey))
        {
            ToolbarSwitch.AddButton(layout, "Compare with…", Icon.Balance, pickCompare);
        }
        else
        {
            // With a comparison running the button needs a way out, so it becomes a menu.
            ToolbarSwitch.Add(layout, $"vs {Resolve(compareKey)}", Icon.Balance,
            [
                ToolbarSwitch.Option("Compare with another account…", active: false, pickCompare),
                ToolbarSwitch.Option("Stop comparing", active: false,
                    NavigateScreenAction.To<ExperienceAnalyzerScreen>()
                        .With(new ExperienceAnalyzerQuery { AccountKey = accountKey, AreaId = q.AreaId })
                        .WithForceReload())
            ]);
        }

        var areas = Areas();
        if (areas.Count > 1)
        {
            var current = areas.FirstOrDefault(a => a.Id == q.AreaId);
            ToolbarSwitch.Add(layout, q.AreaId == 0 ? "All websites" : current.Name ?? "All websites", Icon.Globe,
                new[] { (Id: 0, Name: "All websites") }.Concat(areas).Select(a =>
                    ToolbarSwitch.Option(a.Name, active: a.Id == q.AreaId,
                        NavigateScreenAction.To<ExperienceAnalyzerScreen>()
                            .With(new ExperienceAnalyzerQuery { AccountKey = accountKey, CompareKey = compareKey, AreaId = a.Id }))));
        }
    }

    private static string Resolve(string key)
    {
        try
        {
            return new Core.Principals.Dw.DwAccountCatalog().Resolve(key)?.DisplayName ?? key;
        }
        catch
        {
            return key;
        }
    }

    private static IReadOnlyList<(int Id, string Name)> Areas()
    {
        try
        {
            return new Core.Permissions.Dw.DwContentSecuritySource()
                .GetAreas()
                .Select(a => (a.Id, a.Name))
                .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}
