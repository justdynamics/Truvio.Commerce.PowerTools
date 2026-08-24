using Truvio.Commerce.PowerTools.Core.Diagnostics;
using Truvio.Commerce.PowerTools.Core.Pim.Rules;

namespace Truvio.Commerce.PowerTools.Core.Pim;

/// <summary>The headline numbers shown on the Catalog quality screen.</summary>
public sealed record PimQuality(
    int ProductsScanned,
    int TotalProductCount,
    int AverageScore,
    int BelowThresholdCount,
    int VariantGapCount,
    int BrokenImageCount,
    int DeadRuleCount,
    IReadOnlyList<(string Field, int Count)> WorstFields,
    IReadOnlyList<Finding> Findings)
{
    public int CriticalCount => Findings.Count(f => f.Severity == FindingSeverity.Critical);

    public int WarningCount => Findings.Count(f => f.Severity == FindingSeverity.Warning);

    /// <summary>One word for the whole catalog, driven by the worst finding present.</summary>
    public string Verdict =>
        CriticalCount > 0 ? "Attention needed"
        : WarningCount > 0 ? "Needs a look"
        : "Healthy";

    public bool Healthy => CriticalCount == 0 && WarningCount == 0;

    /// <summary>The one field worth fixing first, or empty when nothing stands out.</summary>
    public string WorstField => WorstFields.Count == 0 ? string.Empty : WorstFields[0].Field;
}

/// <summary>
/// Runs every PIM rule over one snapshot and derives the quality summary. Ordering is stable —
/// severity first, then rule id, then entity — so the same catalog always renders the same
/// list. Mirrors <see cref="Operations.OperationsHealthEngine"/> one-for-one.
/// </summary>
public sealed class PimQualityEngine
{
    private readonly IReadOnlyList<IPimRule> _rules;
    private readonly int _threshold;

    public PimQualityEngine()
        : this(Settings.PowerToolsSettings.Defaults)
    {
    }

    public PimQualityEngine(IReadOnlyList<IPimRule> rules, int threshold = DefaultThreshold)
    {
        _rules = rules;
        _threshold = threshold;
    }

    /// <summary>Every rule, with the thresholds the admin configured in PowerTools settings.</summary>
    public PimQualityEngine(Settings.PowerToolsSettings settings)
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
            Settings.PowerToolsSettings.Positive(settings.PimCompletenessThreshold, DefaultThreshold))
    {
    }

    public const int DefaultThreshold = Settings.PowerToolsSettingKeys.Defaults.PimCompletenessThreshold;

    /// <summary>
    /// A language may trail the default by this many score points before PIM-W3 fires. Derived
    /// from the completeness threshold rather than configured separately: an install that
    /// accepts 60% completeness accepts a 40-point spread between its language layers.
    /// </summary>
    public static int LanguagePointsThreshold(Settings.PowerToolsSettings settings) =>
        Math.Max(5, 100 - Settings.PowerToolsSettings.Positive(settings.PimCompletenessThreshold, DefaultThreshold));

    public static IncompleteProductRule Incomplete(Settings.PowerToolsSettings settings) =>
        new(Settings.PowerToolsSettings.Positive(settings.PimCompletenessThreshold, DefaultThreshold));

    public static CommonFieldGapRule CommonFieldGap(Settings.PowerToolsSettings settings) =>
        new(Settings.PowerToolsSettings.Positive(settings.PimCommonGapPercent, Settings.PowerToolsSettingKeys.Defaults.PimCommonGapPercent));

    public static LanguageGapRule LanguageGap(Settings.PowerToolsSettings settings) =>
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
