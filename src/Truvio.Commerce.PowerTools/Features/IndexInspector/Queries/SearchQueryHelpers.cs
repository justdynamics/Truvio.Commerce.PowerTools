using System.Globalization;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Core;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Dw;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Queries;

/// <summary>Shared helpers for the Search-section queries.</summary>
internal static class SearchQueryHelpers
{
    public static SearchCatalog Catalog() => SearchCatalog.From(new DwSearchSource());

    public static string HealthText(IndexSpec index) => index.Health switch
    {
        IndexHealth.Ok => "OK",
        IndexHealth.Stale => "Stale",
        IndexHealth.NeverBuilt => "Never built",
        IndexHealth.Failed => "Failed",
        _ => index.Health.ToString()
    };

    public static string When(DateTime? value) =>
        value.HasValue ? value.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : "-";

    public static bool Matches(string? search, params string?[] haystack) =>
        string.IsNullOrWhiteSpace(search) ||
        haystack.Any(h => !string.IsNullOrEmpty(h) && h.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));
}
