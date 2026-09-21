using System.Globalization;
using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Models;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Queries;

/// <summary>Every repository's indexes, with builder, size and build health.</summary>
public sealed class IndexListQuery : DataQueryListBase<IndexListModel, IndexListModel, DataListViewModel<IndexListModel>>
{
    protected override IEnumerable<IndexListModel>? GetListItems()
    {
        var catalog = SearchQueryHelpers.Catalog();

        return catalog.Indexes
            .Where(index => SearchQueryHelpers.Matches(Search, index.Name, index.Repository, index.BuilderType))
            .Select(index => new IndexListModel
            {
                RepositoryName = index.Repository,
                Item = index.Item,
                HealthKind = index.Health.ToString(),
                Repository = index.Repository,
                Index = index.Name,
                Builder = string.IsNullOrEmpty(index.BuilderType) ? "-" : index.BuilderType,
                Fields = index.Fields.Count.ToString(CultureInfo.InvariantCulture),
                LastBuild = SearchQueryHelpers.When(index.LastBuild),
                Status = SearchQueryHelpers.HealthText(index)
            })
            .ToList();
    }

    protected override IEnumerable<IndexListModel> MapModels(IEnumerable<IndexListModel> items) => items;

    protected override DataListViewModel<IndexListModel> MakeListModel() => new();
}
