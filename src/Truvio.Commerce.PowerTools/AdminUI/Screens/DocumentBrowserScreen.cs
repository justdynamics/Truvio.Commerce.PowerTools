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
using Icon = Dynamicweb.CoreUI.Icons.Icon;

namespace Truvio.Commerce.PowerTools.AdminUI.Screens;

/// <summary>
/// Step 2 of the document browser: the documents an index instance actually holds. A list
/// screen so the toolbar search box can drive the free-text query against the live index; the
/// full field dump of one document lives on <see cref="DocumentDetailScreen"/>.
/// </summary>
public sealed class DocumentBrowserScreen : ListScreenBase<DocumentRowModel>
{
    internal static readonly int[] TakePresets = [10, 25, 50];

    private DocumentBrowserQuery Q => Query as DocumentBrowserQuery ?? new DocumentBrowserQuery();

    protected override string GetScreenName() => "Documents";

#if DW_HAS_SCREEN_EXPLANATION
    protected override string? GetScreenExplanation() =>
        "Search runs as a free-text query against the live index; pick a row to see every stored field";
#endif

    protected override IEnumerable<ListViewMapping> GetViewMappings() =>
    [
        new RowViewMapping
        {
            Columns =
            [
                CreateMapping(m => m.Key),
                CreateMapping(m => m.Label),
                CreateMapping(m => m.Summary),
                CreateMapping(m => m.Match)
            ]
        }
    ];

    protected override Cell? GetCell(string propertyName, DocumentRowModel model) =>
        propertyName == nameof(DocumentRowModel.Match) && model.Match is "Match" or "Differs" or "Deleted"
            ? Cell.MakeCell(new Badge
            {
                Value = model.Match,
                BadgeType = model.Match switch
                {
                    "Match" => BadgeType.Success,
                    _ => BadgeType.Danger
                }
            })
            : null;

    protected override ActionBase? GetListItemPrimaryAction(DocumentRowModel model)
    {
        if (!PowerToolsAccess.CanUseSearchInspector() || model.Ordinal <= 0)
            return null;

        var q = Q;
        return NavigateScreenAction.To<DocumentDetailScreen>()
            .With(new DocumentDetailQuery
            {
                Repository = model.RepositoryName,
                Item = model.Item,
                Text = q.Search ?? string.Empty,
                Field = q.Field,
                Value = q.Value,
                Ordinal = model.Ordinal
            });
    }

    protected override IEnumerable<ActionGroup>? GetScreenActions()
    {
        var q = Q;

        var groups = new List<ActionGroup>
        {
            new()
            {
                Nodes =
                [
                    new ActionNode
                    {
                        Name = "Select another index",
                        Icon = Icon.Database,
                        NodeAction = NavigateScreenAction.To<IndexPickScreen>().With(new IndexPickQuery())
                    },
                    new ActionNode
                    {
                        Name = "Index detail",
                        Icon = Icon.Info,
                        NodeAction = NavigateScreenAction.To<IndexDetailScreen>()
                            .With(new IndexDetailQuery { Repository = q.Repository, Item = q.Item })
                    }
                ]
            }
        };

        return groups;
    }

    internal static NavigateScreenAction Navigate(DocumentBrowserQuery q, Action<DocumentBrowserQuery> change)
    {
        var next = new DocumentBrowserQuery
        {
            Repository = q.Repository,
            Item = q.Item,
            Field = q.Field,
            Value = q.Value,
            Take = q.Take,
            Compare = q.Compare,
            Search = q.Search
        };
        change(next);
        return NavigateScreenAction.To<DocumentBrowserScreen>().With(next);
    }
}

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


/// <summary>
/// Document count and database comparison as toolbar controls, labelled with the state in
/// effect - see <see cref="ToolbarSwitch"/>.
/// </summary>
public sealed class DocumentBrowserToolbarInjector : ScreenInjector<DocumentBrowserScreen>
{
    public override void OnAfter(DocumentBrowserScreen screen, Dynamicweb.CoreUI.UiComponentBase content)
    {
        if (content is not Dynamicweb.CoreUI.Layout.ScreenLayout layout)
            return;

        if (screen.Query is not DocumentBrowserQuery q || string.IsNullOrEmpty(q.Repository) || string.IsNullOrEmpty(q.Item))
            return;

        ToolbarSwitch.Add(layout, $"{q.Take} docs", Icon.ListUl,
            DocumentBrowserScreen.TakePresets.Select(take => ToolbarSwitch.Option($"Show {take} documents",
                active: take == q.Take, DocumentBrowserScreen.Navigate(q, x => x.Take = take))));

        if (DocumentBrowserQuery.IsProductIndex(q.Repository, q.Item))
        {
            ToolbarSwitch.Add(layout, q.Compare ? "Compare on" : "Compare off", Icon.Balance,
            [
                ToolbarSwitch.Option("Compare with the database", active: q.Compare, DocumentBrowserScreen.Navigate(q, x => x.Compare = true)),
                ToolbarSwitch.Option("Stop comparing with the database", active: !q.Compare, DocumentBrowserScreen.Navigate(q, x => x.Compare = false))
            ]);
        }
    }
}
