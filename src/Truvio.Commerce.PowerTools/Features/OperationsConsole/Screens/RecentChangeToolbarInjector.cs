using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Icons;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Queries;
using Truvio.Commerce.PowerTools.Shared.AdminUI;

namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Screens;

/// <summary>The window selector as a toolbar control labelled with the days in effect.</summary>
public sealed class RecentChangeToolbarInjector : ScreenInjector<RecentChangeListScreen>
{
    public override void OnAfter(RecentChangeListScreen screen, Dynamicweb.CoreUI.UiComponentBase content)
    {
        if (content is not Dynamicweb.CoreUI.Layout.ScreenLayout layout)
            return;

        var q = screen.Query as RecentChangeListQuery ?? new RecentChangeListQuery();
        var effective = q.EffectiveDays();

        ToolbarSwitch.Add(layout, effective == 1 ? "Last 24 hours" : $"Last {effective} days", Icon.CalendarAlt,
            RecentChangeListScreen.DayPresets.Select(days => ToolbarSwitch.Option(
                days == 1 ? "Last 24 hours" : $"Last {days} days",
                active: days == effective,
                NavigateScreenAction.To<RecentChangeListScreen>()
                    .With(new RecentChangeListQuery { Days = days, Search = q.Search }))));
    }
}
