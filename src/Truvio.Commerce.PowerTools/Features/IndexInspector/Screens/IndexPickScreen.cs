using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Models;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Queries;
using Truvio.Commerce.PowerTools.Shared.Security;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Screens;

/// <summary>Step 1 of the document browser: pick the index to read.</summary>
public sealed class IndexPickScreen : ListScreenBase<IndexPickModel>
{
    protected override string GetScreenName() => "Document browser - indexes";

#if DW_HAS_SCREEN_EXPLANATION
    protected override string? GetScreenExplanation() =>
        "Pick an index to read its documents; an index that has never been built has nothing to show";
#endif

    protected override IEnumerable<ListViewMapping> GetViewMappings() =>
    [
        new RowViewMapping
        {
            Columns =
            [
                CreateMapping(m => m.Repository),
                CreateMapping(m => m.Index),
                CreateMapping(m => m.Instance),
                CreateMapping(m => m.Documents),
                CreateMapping(m => m.Status)
            ]
        }
    ];

    protected override Cell? GetCell(string propertyName, IndexPickModel model) =>
        propertyName == nameof(IndexPickModel.Status)
            ? SearchBadges.Health(model.HealthKind, model.Status)
            : null;

    protected override ActionBase? GetListItemPrimaryAction(IndexPickModel model) =>
        PowerToolsAccess.CanUseSearchInspector()
            ? NavigateScreenAction.To<DocumentBrowserScreen>()
                .With(new DocumentBrowserQuery { Repository = model.RepositoryName, Item = model.Item })
            : null;
}
