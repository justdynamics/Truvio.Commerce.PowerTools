using System.Globalization;
using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.Features.PimQuality.Core;
using Truvio.Commerce.PowerTools.Features.PimQuality.Models;
using Truvio.Commerce.PowerTools.Features.Settings.Core;
using Truvio.Commerce.PowerTools.Features.Settings.Dw;
using Truvio.Commerce.PowerTools.Shared.AdminUI;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Queries;

/// <summary>
/// The Completeness explorer's rows: DW's completeness for every product family in scope,
/// worst first. Only <see cref="GroupId"/> and <see cref="LanguageId"/> are public — every
/// public property of a query is serialised into the screen URL, so computed values stay
/// methods.
/// </summary>
public sealed class PimCompletenessQuery : DataQueryListBase<PimCompletenessModel, PimCompletenessModel, DataListViewModel<PimCompletenessModel>>
{
    /// <summary>Product group to scan; empty = the whole catalog.</summary>
    public string GroupId { get; set; } = string.Empty;

    /// <summary>Language the scores are read in; empty = DW's default language.</summary>
    public string LanguageId { get; set; } = string.Empty;

    /// <summary>Resolves a toolbar group pick — see <see cref="PickStore"/>.</summary>
    public string GroupPickToken { get; set; } = string.Empty;

    private void ResolvePicks()
    {
        if (!string.IsNullOrEmpty(GroupPickToken) && PickStore.Get(GroupPickToken) is { Length: > 0 } picked)
            GroupId = string.Equals(picked, PimPicks.WholeCatalog, StringComparison.Ordinal) ? string.Empty : picked;
    }

    public PimScope GetScope() => PimQueryHelpers.Scope(GroupId, LanguageId, Search);

    protected override IEnumerable<PimCompletenessModel>? GetListItems()
    {
        ResolvePicks();

        var threshold = PowerToolsSettings.Positive(
            DwPowerToolsSettings.Current.PimCompletenessThreshold, PimQualityEngine.DefaultThreshold);

        var (products, total) = PimQueryHelpers.Source().GetProductQuality(GetScope());

        var items = products.Select(p => new PimCompletenessModel
        {
            ProductId = p.ProductId,
            LanguageId = p.LanguageId,
            ScoreValue = p.Score,
            Number = string.IsNullOrEmpty(p.Number) ? p.ProductId : p.Number,
            Name = p.DisplayName,
            Score = PimQueryHelpers.Percent(p.Score),
            WorstRule = string.IsNullOrEmpty(p.WorstRule) ? "-" : p.WorstRule,
            MissingCount = p.MissingFields.Count.ToString(CultureInfo.InvariantCulture),
            MissingFields = p.MissingFields.Count == 0 ? "-" : string.Join(", ", p.MissingFields.Take(3)) +
                (p.MissingFields.Count > 3 ? $" +{p.MissingFields.Count - 3}" : string.Empty)
        }).ToList();

        // Never truncate silently: the same trailing row the product picker uses.
        if (total > items.Count)
        {
            items.Add(new PimCompletenessModel
            {
                ProductId = string.Empty,
                Number = "...",
                Name = $"{total - items.Count} more products not shown - use the search to narrow the list",
                Score = string.Empty,
                WorstRule = string.Empty,
                MissingCount = string.Empty,
                MissingFields = string.Empty
            });
        }

        _ = threshold;
        return items;
    }

    protected override IEnumerable<PimCompletenessModel> MapModels(IEnumerable<PimCompletenessModel> items) => items;

    protected override DataListViewModel<PimCompletenessModel> MakeListModel() => new();
}
