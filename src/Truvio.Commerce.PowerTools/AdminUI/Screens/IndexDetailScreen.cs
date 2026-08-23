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
/// One index in full. An overview screen rather than a list: the schema, the builder settings
/// and the instance states are long text, and the CoreUI list grid splits the width evenly
/// between columns and clips.
/// </summary>
public sealed class IndexDetailScreen : OverviewScreenBase<IndexDetailModel>
{
    private IndexDetailQuery Q => Query as IndexDetailQuery ?? new IndexDetailQuery();

    protected override string GetScreenName() =>
        Model is null || string.IsNullOrEmpty(Model.Title) ? "Index" : $"Index: {Model.Title}";

    protected override void BuildOverviewScreen()
    {
        var model = Model;
        if (model is null)
            return;

        if (!string.IsNullOrEmpty(model.Error))
        {
            AddComponent(new Alert { Value = model.Error, Icon = Icon.ExclamationTriangle }, "Index unavailable", Group.GroupWidth.Col_12);
            return;
        }

        SetInfobar(new InfoBar
        {
            Icon = Icon.Database,
            Information = new Dictionary<string, CardInfo.InfoValue>
            {
                ["Repository"] = new(model.Repository),
                ["Builder"] = new(model.Builder),
                ["Fields"] = new(model.FieldCount),
                ["Documents"] = new(model.Documents),
                ["Status"] = new(new Badge
                {
                    Value = model.Status,
                    BadgeType = model.HealthKind switch
                    {
                        "Ok" => BadgeType.Success,
                        "Stale" => BadgeType.Warning,
                        _ => BadgeType.Danger
                    }
                })
            }
        });

        if (model.HealthKind != "Ok" && !string.IsNullOrEmpty(model.StatusDetail))
            AddComponent(new Alert { Value = model.StatusDetail, Icon = Icon.ExclamationTriangle }, "Build status", Group.GroupWidth.Col_12);

        foreach (var section in model.Sections)
            AddComponent(new HtmlBlock { Value = section.Html }, section.Heading, Group.GroupWidth.Col_12);
    }

    protected override IEnumerable<ActionGroup>? GetScreenActions()
    {
        var q = Q;

        return
        [
            new ActionGroup
            {
                Nodes =
                [
                    new ActionNode
                    {
                        Name = "Browse documents",
                        Icon = Icon.Table,
                        NodeAction = NavigateScreenAction.To<DocumentBrowserScreen>()
                            .With(new DocumentBrowserQuery { Repository = q.Repository, Item = q.Item })
                    },
                    new ActionNode
                    {
                        Name = "All indexes",
                        Icon = Icon.Database,
                        NodeAction = NavigateScreenAction.To<IndexListScreen>().With(new IndexListQuery())
                    },
                    new ActionNode
                    {
                        Name = "Query linter",
                        Icon = Icon.Bug,
                        NodeAction = NavigateScreenAction.To<QueryLintScreen>().With(new QueryLintQuery())
                    }
                ]
            }
        ];
    }
}
