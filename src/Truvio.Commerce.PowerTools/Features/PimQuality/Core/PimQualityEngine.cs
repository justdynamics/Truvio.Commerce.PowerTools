using Truvio.Commerce.PowerTools.Features.OperationsConsole.Core;
using Truvio.Commerce.PowerTools.Features.PimQuality.Core.Rules;
using Truvio.Commerce.PowerTools.Features.Settings.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Core;

/// <summary>
/// Runs every PIM rule over one snapshot and derives the quality summary. Ordering is stable —
/// severity first, then rule id, then entity — so the same catalog always renders the same
/// list. Mirrors <see cref="OperationsHealthEngine"/> one-for-one.
/// </summary>
public sealed class PimQualityEngine
{
    private readonly IReadOnlyList<IPimRule> _rules;
    private readonly int _threshold;

    public PimQualityEngine()
        : this(PowerToolsSettings.Defaults)
    {
    }

    public PimQualityEngine(IReadOnlyList<IPimRule> rules, int threshold = DefaultThreshold)
    {
        _rules = rules;
        _threshold = threshold;
    }

    /// <summary>Every rule, with the thresholds the admin configured in PowerTools settings.</summary>
    public PimQualityEngine(PowerToolsSettings settings)
        : this(
            [
                Incomplete(settings),
                CommonFieldGap(settings),
                LanguageGap(settings),
                new VariantGapRule(),
                new DuplicateAssetRule(),
                new DeadCompletionRuleRule(),
                new UnusedCategoryRule(),
                new BrokenImageRule()
            ],
            PowerToolsSettings.Positive(settings.PimCompletenessThreshold, DefaultThreshold))
    {
    }

    public const int DefaultThreshold = PowerToolsSettingKeys.Defaults.PimCompletenessThreshold;

    /// <summary>
    /// A language may trail the default by this many score points before PIM-W3 fires. Derived
    /// from the completeness threshold rather than configured separately: an install that
    /// accepts 60% completeness accepts a 40-point spread between its language layers.
    /// </summary>
    public static int LanguagePointsThreshold(PowerToolsSettings settings) =>
        Math.Max(5, 100 - PowerToolsSettings.Positive(settings.PimCompletenessThreshold, DefaultThreshold));

    public static IncompleteProductRule Incomplete(PowerToolsSettings settings) =>
        new(PowerToolsSettings.Positive(settings.PimCompletenessThreshold, DefaultThreshold));

    public static CommonFieldGapRule CommonFieldGap(PowerToolsSettings settings) =>
        new(PowerToolsSettings.Positive(settings.PimCommonGapPercent, PowerToolsSettingKeys.Defaults.PimCommonGapPercent));

    public static LanguageGapRule LanguageGap(PowerToolsSettings settings) =>
        new(LanguagePointsThreshold(settings));

    public IReadOnlyList<Finding> Run(PimSnapshot snapshot)
    {
        var findings = new List<Finding>();
        foreach (var rule in _rules)
        {
            try
            {
                findings.AddRange(rule.Evaluate(snapshot));
            }
            catch (Exception ex)
            {
                // A rule that cannot read one part of the catalog must not hide the others.
                findings.Add(new Finding(
                    "PIM-E1",
                    FindingSeverity.Info,
                    PimEntities.CompletionRule,
                    rule.GetType().Name,
                    rule.GetType().Name,
                    "Rule could not be evaluated",
                    ex.Message));
            }
        }

        return findings
            .OrderByDescending(f => f.Severity)
            .ThenBy(f => f.RuleId, StringComparer.Ordinal)
            .ThenBy(f => f.EntityDisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public PimQuality Summarise(PimSnapshot snapshot)
    {
        var findings = Run(snapshot);

        return new PimQuality(
            ProductsScanned: snapshot.Products.Count,
            TotalProductCount: snapshot.TotalProductCount,
            AverageScore: snapshot.AverageScore,
            BelowThresholdCount: snapshot.Products.Count(p => p.Score < _threshold),
            VariantGapCount: snapshot.VariantGaps.Count(g => g.HasGap),
            BrokenImageCount: snapshot.BrokenImages.Count,
            DeadRuleCount: snapshot.Rules.Count(r => r.IsDead),
            WorstFields: CommonFieldGapRule.Rank(snapshot),
            Findings: findings);
    }
}
