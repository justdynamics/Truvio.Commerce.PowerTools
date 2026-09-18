using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Displays.Widgets;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Icon = Dynamicweb.CoreUI.Icons.Icon;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Models;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Queries;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Screens;

/// <summary>Step 3: one document, every stored field, and where it disagrees with the database.</summary>
public sealed class DocumentDetailScreen : OverviewScreenBase<DocumentDetailModel>
{
    private DocumentDetailQuery Q => Query as DocumentDetailQuery ?? new DocumentDetailQuery();

    protected override string GetScreenName() =>
        Model is null || string.IsNullOrEmpty(Model.Key) ? "Document" : $"Document {Model.Key}";

    protected override void BuildOverviewScreen()
    {
        var model = Model;
        if (model is null)
            return;

        if (!string.IsNullOrEmpty(model.Error))
        {
            AddComponent(new Alert { Value = model.Error, Icon = Icon.ExclamationTriangle }, "Document unavailable", Group.GroupWidth.Col_12);
            return;
        }

        SetInfobar(new InfoBar
        {
            Icon = Icon.Table,
            Information = new Dictionary<string, CardInfo.InfoValue>
            {
                ["Index"] = new(model.IndexName),
                ["Instance"] = new(model.Instance),
                ["Fields"] = new(model.FieldCount),
                ["Database"] = new(new Badge
                {
                    Value = model.Match,
                    BadgeType = model.MatchKind switch
                    {
                        "Match" => BadgeType.Success,
                        "Differs" => BadgeType.Danger,
                        "MissingInDatabase" => BadgeType.Danger,
                        _ => BadgeType.Muted
                    }
                })
            }
        });

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
                        Name = "Back to documents",
                        Icon = Icon.Table,
                        NodeAction = NavigateScreenAction.To<DocumentBrowserScreen>()
                            .With(new DocumentBrowserQuery
                            {
                                Repository = q.Repository,
                                Item = q.Item,
                                Field = q.Field,
                                Value = q.Value,
                                Search = q.Text
                            })
                    },
                    new ActionNode
                    {
                        Name = "Index detail",
                        Icon = Icon.Database,
                        NodeAction = NavigateScreenAction.To<IndexDetailScreen>()
                            .With(new IndexDetailQuery { Repository = q.Repository, Item = q.Item })
                    }
                ]
            }
        ];
    }
}
