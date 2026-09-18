using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Icons;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Models;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Queries;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Screens;

/// <summary>One row per index field, with everything that references it.</summary>
public sealed class FieldUsageScreen : ListScreenBase<FieldUsageModel>
{
    private FieldUsageQuery Q => Query as FieldUsageQuery ?? new FieldUsageQuery();

    protected override string GetScreenName() => "Overview";

#if DW_HAS_SCREEN_EXPLANATION
    protected override string? GetScreenExplanation() =>
        "Search by field name; 'Dangling' means a query references a field the index does not have, " +
        "'Unused' means an indexed field nothing ever asks for";
#endif

    protected override IEnumerable<ActionGroup>? GetScreenActions() =>
    [
        new ActionGroup
        {
            Nodes =
            [
                new ActionNode
                {
                    Name = "Repositories & indexes",
                    Icon = Icon.Database,
                    NodeAction = NavigateScreenAction.To<IndexListScreen>().With(new IndexListQuery())
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
                // Status first, like the Warnings screen: field system names are long and
                // push the trailing columns out of view.
                CreateMapping(m => m.Status),
                CreateMapping(m => m.Field),
                CreateMapping(m => m.Index),
                CreateMapping(m => m.Type),
                CreateMapping(m => m.UsedBy)
            ]
        }
    ];

    protected override Cell? GetCell(string propertyName, FieldUsageModel model) =>
        propertyName == nameof(FieldUsageModel.Status)
            ? SearchBadges.FieldStatus(model.StatusKind)
            : null;
}
