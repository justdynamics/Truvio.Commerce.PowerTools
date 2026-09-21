using Dynamicweb.CoreUI;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Screens;
using Icon = Dynamicweb.CoreUI.Icons.Icon;
using Truvio.Commerce.PowerTools.Features.ContentAccess.Dw;
using Truvio.Commerce.PowerTools.Features.ExperienceAnalyzer.Queries;
using Truvio.Commerce.PowerTools.Shared.AdminUI;
using Truvio.Commerce.PowerTools.Shared.Principals;

namespace Truvio.Commerce.PowerTools.Features.ExperienceAnalyzer.Screens;

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
            new AccountSelectorProvider(), accountToken,
            NavigateScreenAction.To<ExperienceAnalyzerScreen>()
                .With(new ExperienceAnalyzerQuery { PickToken = accountToken, CompareKey = compareKey, AreaId = q.AreaId }));

        var compareToken = Guid.NewGuid().ToString("N");
        var pickCompare = OpenSlideOverAction
            .To<Dynamicweb.Application.UI.Screens.SelectorScreen>()
            .With(new Dynamicweb.Application.UI.Queries.SelectorDataByProviderQuery(new AccountSelectorProvider()))
            .WithOnSelectAction(
                RunCommandAction
                    .For(new ToolbarPickCommand { Token = compareToken })
                    .WithCommandProperty(nameof(ToolbarPickCommand.PickedId))
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
            return new DwAccountCatalog().Resolve(key)?.DisplayName ?? key;
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
            return new DwContentSecuritySource()
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
