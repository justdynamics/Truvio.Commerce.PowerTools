using System.Globalization;
using Truvio.Commerce.PowerTools.Features.PimQuality.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Core.Rules;

/// <summary>
/// PIM-W3: a language layer materially behind the default one. Reported per language, not per
/// product — "Danish is 40 points behind" is the actionable sentence; 400 product rows are not.
/// </summary>
public sealed class LanguageGapRule(int pointsThreshold) : IPimRule
{
    public const string Id = "PIM-W3";

    /// <summary>Score points a language may trail the default by before it is reported.</summary>
    public int PointsThreshold { get; } = pointsThreshold;

    public IEnumerable<Finding> Evaluate(PimSnapshot snapshot)
    {
        var defaultLanguage = snapshot.Languages.FirstOrDefault();
        if (string.IsNullOrEmpty(defaultLanguage) || snapshot.Products.Count == 0)
            yield break;

        foreach (var language in snapshot.Languages.Skip(1))
        {
            var pairs = snapshot.Products
                .Where(p => p.ScorePerLanguage.ContainsKey(defaultLanguage) && p.ScorePerLanguage.ContainsKey(language))
                .ToList();

            if (pairs.Count == 0)
                continue;

            var baseline = pairs.Average(p => p.ScorePerLanguage[defaultLanguage]);
            var actual = pairs.Average(p => p.ScorePerLanguage[language]);
            var delta = baseline - actual;
            if (delta < PointsThreshold)
                continue;

            var worst = pairs
                .OrderBy(p => p.ScorePerLanguage[language] - p.ScorePerLanguage[defaultLanguage])
                .Take(3)
                .Select(Format.Product)
                .ToList();

            yield return new Finding(
                Id,
                FindingSeverity.Warning,
                PimEntities.Language,
                language,
                language,
                $"Language layer averages {actual:0}% against {baseline:0}% in {defaultLanguage} ({delta:0} points behind)",
                $"Measured over {pairs.Count} product(s). Furthest behind: {string.Join("; ", worst)}.",
                Subject: language);
        }
    }
}
