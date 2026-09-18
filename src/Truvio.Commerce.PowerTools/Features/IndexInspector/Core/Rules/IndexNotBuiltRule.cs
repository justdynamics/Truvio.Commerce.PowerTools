using System.Globalization;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core.Rules;

/// <summary>
/// IDX-W17 — the index has never been built, its last build failed, or it has not been
/// refreshed for a day. Mirrors the platform's own threshold: <c>IndexHelper</c> flags an
/// index as a warning once <c>lastBuildTime &lt; DateTime.Now.AddHours(-24)</c>.
/// </summary>
public sealed class IndexNotBuiltRule : IQueryLintRule
{
    public string RuleId => "IDX-W17";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var index in catalog.Indexes)
        {
            var (severity, title) = index.Health switch
            {
                IndexHealth.NeverBuilt => (FindingSeverity.Critical, "Index has never been built"),
                IndexHealth.Failed => (FindingSeverity.Critical, "Last index build failed"),
                IndexHealth.Stale => (FindingSeverity.Warning, "Index has not been rebuilt recently"),
                _ => (FindingSeverity.Info, string.Empty)
            };

            if (title.Length == 0)
                continue;

            var when = index.LastBuild.HasValue
                ? $"Last build: {index.LastBuild.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}. "
                : "There is no build history for any instance. ";

            yield return new Finding(
                RuleId,
                severity,
                SearchEntityNames.Index,
                index.Key,
                catalog.Describe(index),
                title,
                when + index.HealthDetail + " Documents served from this index can be out of date.");
        }
    }
}
