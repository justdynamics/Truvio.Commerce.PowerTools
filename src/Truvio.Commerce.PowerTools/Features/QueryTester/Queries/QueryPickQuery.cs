using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Core;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Queries;
using Truvio.Commerce.PowerTools.Features.QueryTester.Models;

namespace Truvio.Commerce.PowerTools.Features.QueryTester.Queries;

/// <summary>Every repository query, with its source index and how many parameters can go blank.</summary>
public sealed class QueryPickQuery : DataQueryListBase<QueryPickModel, QueryPickModel, DataListViewModel<QueryPickModel>>
{
    protected override IEnumerable<QueryPickModel>? GetListItems()
    {
        var catalog = SearchQueryHelpers.Catalog();

        return catalog.Queries
            .Where(query => SearchQueryHelpers.Matches(Search, query.Name, query.Repository, query.SourceItem))
            .Select(query =>
            {
                var index = catalog.IndexFor(query);
                var withDefault = query.Parameters.Count(p => p.HasDefault);

                return new QueryPickModel
                {
                    RepositoryName = query.Repository,
                    Item = query.Item,
                    HealthKind = index?.Health.ToString() ?? "Missing",
                    Repository = query.Repository,
                    Query = query.Name,
                    Source = index is null ? $"{query.SourceKey} (missing)" : index.Name,
                    Parameters = query.Parameters.Count == 0
                        ? "0"
                        : $"{query.Parameters.Count} ({withDefault} with a default)",
                    Status = index is null
                        ? "Source missing"
                        : $"{SearchQueryHelpers.HealthText(index)}{OnlineSuffix(index)}"
                };
            })
            .ToList();
    }

    private static string OnlineSuffix(IndexSpec index) =>
        string.IsNullOrEmpty(index.OnlineInstance) ? " - no online instance" : $" - {index.OnlineInstance}";

    protected override IEnumerable<QueryPickModel> MapModels(IEnumerable<QueryPickModel> items) => items;

    protected override DataListViewModel<QueryPickModel> MakeListModel() => new();
}
