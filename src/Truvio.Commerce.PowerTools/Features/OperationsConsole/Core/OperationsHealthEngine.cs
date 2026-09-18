using Truvio.Commerce.PowerTools.Features.OperationsConsole.Core.Rules;
using Truvio.Commerce.PowerTools.Features.Settings.Core;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Core;

/// <summary>
/// Runs every Operations rule over one snapshot and derives the health summary. Ordering is
/// stable — severity first, then rule id, then entity — so the same install always renders the
/// same list.
/// </summary>
public sealed class OperationsHealthEngine
{
    private readonly IReadOnlyList<IOperationsRule> _rules;

    public OperationsHealthEngine()
        : this([new FailingTaskRule(), new StaleTaskRule(), new BrokenActivityLinkRule(), new LogGrowthRule(), new TableBloatRule(),
                new CurrencyConfigurationRule(PowerToolsSettingKeys.Defaults.RateDeviationPercent)])
    {
    }

    public OperationsHealthEngine(IReadOnlyList<IOperationsRule> rules) => _rules = rules;

    /// <summary>Every rule, with the thresholds the admin configured in PowerTools settings.</summary>
    public OperationsHealthEngine(PowerToolsSettings settings)
        : this([new FailingTaskRule(), StaleTask(settings), new BrokenActivityLinkRule(), LogGrowth(settings), TableBloat(settings),
                CurrencyConfiguration(settings)])
    {
    }

    public static StaleTaskRule StaleTask(PowerToolsSettings settings) =>
        new(settings.StaleTaskIntervalMultiplier);

    public static LogGrowthRule LogGrowth(PowerToolsSettings settings) =>
        new(settings.LogFolderWarningMb * 1024L * 1024L, settings.LogFolderCriticalMb * 1024L * 1024L);

    public static TableBloatRule TableBloat(PowerToolsSettings settings) =>
        new(settings.TableSharePercent / 100d);

    public static CurrencyConfigurationRule CurrencyConfiguration(PowerToolsSettings settings) =>
        new(settings.RateDeviationPercent);

    public IReadOnlyList<Finding> Run(OperationsSnapshot snapshot)
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
                // A rule that cannot read one part of the install must not hide the others.
                findings.Add(new Finding(
                    "OPS-E1",
                    FindingSeverity.Info,
                    OperationsEntities.Configuration,
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

    public OperationsHealth Summarise(OperationsSnapshot snapshot)
    {
        var findings = Run(snapshot);

        var bloat = findings
            .Where(f => f.EntityName is OperationsEntities.DatabaseTable or OperationsEntities.LogFolder)
            .OrderByDescending(f => f.Severity)
            .FirstOrDefault();

        return new OperationsHealth(
            TaskCount: snapshot.Tasks.Count,
            EnabledTaskCount: snapshot.Tasks.Count(t => t.Enabled),
            FailingTaskCount: findings.Count(f => f.RuleId == FailingTaskRule.Id),
            StaleTaskCount: findings.Count(f => f.RuleId is StaleTaskRule.StaleId or StaleTaskRule.NeverRunId),
            ActivityCount: snapshot.Activities.Count,
            BrokenLinkCount: findings.Count(f => f.RuleId == BrokenActivityLinkRule.BrokenId),
            LogBytes: snapshot.TotalLogBytes,
            DatabaseBytes: snapshot.TotalTableBytes,
            LargestBloatFinding: bloat,
            Findings: findings);
    }
}
