using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Icons;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.AdminUI.Queries;

namespace Truvio.Commerce.PowerTools.AdminUI.Screens;

/// <summary>
/// Who changed what recently. The window is a screen action so every state is a shareable URL.
/// </summary>
public sealed class RecentChangeListScreen : ListScreenBase<RecentChangeModel>
{
    private static readonly int[] DayPresets = [1, 7, 30, 90, 365];

    private RecentChangeListQuery Q => Query as RecentChangeListQuery ?? new RecentChangeListQuery();

    protected override string GetScreenName() =>
        $"Operations - recent changes ({Q.EffectiveDays()} days)";

#if DW_HAS_SCREEN_EXPLANATION
    protected override string? GetScreenExplanation() =>
        "From the admin command log, the audit trail and configuration file timestamps; \"unknown\" means the source keeps no author";
#endif

    protected override IEnumerable<ActionGroup>? GetScreenActions()
    {
        var q = Q;
        return
        [
            new ActionGroup
            {
                Nodes = DayPresets.Select(days => new ActionNode
                {
                    Name = days == 1 ? "Last 24 hours" : $"Last {days} days",
                    Icon = Icon.CalendarAlt,
                    NodeAction = NavigateScreenAction.To<RecentChangeListScreen>()
                        .With(new RecentChangeListQuery { Days = days, Search = q.Search })
                }).ToList()
            }
        ];
    }

    protected override IEnumerable<ListViewMapping> GetViewMappings() =>
    [
        new RowViewMapping
        {
            Columns =
            [
                CreateMapping(m => m.When),
                CreateMapping(m => m.Ago),
                CreateMapping(m => m.Who),
                CreateMapping(m => m.What),
                CreateMapping(m => m.Source)
            ]
        }
    ];

    protected override Cell? GetCell(string propertyName, RecentChangeModel model) =>
        propertyName == nameof(RecentChangeModel.Source) && !string.IsNullOrEmpty(model.SourceKind)
            ? OpsBadges.ChangeSource(model.Source)
            : null;
}
