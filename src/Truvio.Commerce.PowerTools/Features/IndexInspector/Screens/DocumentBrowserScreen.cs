using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Icon = Dynamicweb.CoreUI.Icons.Icon;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Models;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Queries;
using Truvio.Commerce.PowerTools.Shared.Security;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Screens;

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
