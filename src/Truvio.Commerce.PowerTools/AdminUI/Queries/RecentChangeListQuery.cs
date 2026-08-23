using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Operations;
using Truvio.Commerce.PowerTools.Core.Operations.Dw;
using Truvio.Commerce.PowerTools.Core.Settings.Dw;
using Truvio.Commerce.PowerTools.Core.Settings;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// Who changed what recently, from the sources DW already keeps. Where a source keeps no
/// author the row says "unknown" — it is never guessed at.
/// </summary>
public sealed class RecentChangeListQuery : DataQueryListBase<RecentChangeModel, RecentChangeModel, DataListViewModel<RecentChangeModel>>
{
    public const int DefaultDays = 30;

    private const int FetchCap = 500;

    /// <summary>How far back to look, in days. Round-trips through the screen URL; 0 = use the configured default.</summary>
    public int Days { get; set; }

    /// <summary>
    /// The window actually used. A method, not a property: CoreUI serialises every public
    /// property of a query into the screen URL.
    /// </summary>
    public int EffectiveDays() =>
        Days > 0 ? Days : PowerToolsSettings.Positive(DwPowerToolsSettings.Current.RecentChangesDays, DefaultDays);

    protected override IEnumerable<RecentChangeModel>? GetListItems()
    {
        var now = DateTime.Now;
        var days = EffectiveDays();
        var changes = new DwOperationsSource().GetRecentChanges(days, FetchCap);

        var search = (Search ?? string.Empty).Trim();

        var items = changes
            .Where(c => search.Length == 0
                     || c.What.Contains(search, StringComparison.OrdinalIgnoreCase)
                     || c.Who.Contains(search, StringComparison.OrdinalIgnoreCase)
                     || c.Where.Contains(search, StringComparison.OrdinalIgnoreCase))
            .Select(c => new RecentChangeModel
            {
                SourceKind = c.Where,
                When = OpsFormat.Absolute(c.When),
                Ago = OpsFormat.Relative(c.When, now),
                Who = c.Who,
                What = c.What,
                Source = c.Where
            })
            .ToList();

        if (items.Count == 0)
        {
            items.Add(new RecentChangeModel
            {
                When = "-",
                Ago = "-",
                Who = "-",
                What = $"No recorded changes in the last {days} day(s)",
                Source = "-"
            });
        }

        return items;
    }

    protected override IEnumerable<RecentChangeModel> MapModels(IEnumerable<RecentChangeModel> items) => items;

    protected override DataListViewModel<RecentChangeModel> MakeListModel() => new();
}
