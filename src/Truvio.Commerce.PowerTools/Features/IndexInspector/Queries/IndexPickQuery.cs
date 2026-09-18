using System.Globalization;
using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Dw;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Models;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Queries;

/// <summary>Index picker for the document browser; shows the live document count per index.</summary>
public sealed class IndexPickQuery : DataQueryListBase<IndexPickModel, IndexPickModel, DataListViewModel<IndexPickModel>>
{
    protected override IEnumerable<IndexPickModel>? GetListItems()
    {
        var catalog = SearchQueryHelpers.Catalog();

        return catalog.Indexes
            .Where(index => SearchQueryHelpers.Matches(Search, index.Name, index.Repository))
            .Select(index =>
            {
                var count = DwIndexDocuments.Count(index.Repository, index.Item);
                return new IndexPickModel
                {
                    RepositoryName = index.Repository,
                    Item = index.Item,
                    HealthKind = index.Health.ToString(),
                    Repository = index.Repository,
                    Index = index.Name,
                    Instance = string.IsNullOrEmpty(index.OnlineInstance) ? "-" : index.OnlineInstance,
                    Documents = count.HasValue ? count.Value.ToString("N0", CultureInfo.InvariantCulture) : "-",
                    Status = SearchQueryHelpers.HealthText(index)
                };
            })
            .ToList();
    }

    protected override IEnumerable<IndexPickModel> MapModels(IEnumerable<IndexPickModel> items) => items;

    protected override DataListViewModel<IndexPickModel> MakeListModel() => new();
}
