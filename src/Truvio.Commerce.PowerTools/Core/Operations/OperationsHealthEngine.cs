using Truvio.Commerce.PowerTools.Core.Diagnostics;
using Truvio.Commerce.PowerTools.Core.Operations.Rules;

namespace Truvio.Commerce.PowerTools.Core.Operations;

/// <summary>The headline numbers shown on the Operations health screen.</summary>
public sealed record OperationsHealth(
    int TaskCount,
    int EnabledTaskCount,
    int FailingTaskCount,
    int StaleTaskCount,
    int ActivityCount,
    int BrokenLinkCount,
    long LogBytes,
    long DatabaseBytes,
    Finding? LargestBloatFinding,
    IReadOnlyList<Finding> Findings)
{
    public int CriticalCount => Findings.Count(f => f.Severity == FindingSeverity.Critical);

    public int WarningCount => Findings.Count(f => f.Severity == FindingSeverity.Warning);

    /// <summary>One word for the whole install, driven by the worst finding present.</summary>
    public string Verdict =>
        CriticalCount > 0 ? "Attention needed"
        : WarningCount > 0 ? "Needs a look"
        : "Healthy";
}

/// <summary>
/// Runs every Operations rule over one snapshot and derives the health summary. Ordering is
/// stable — severity first, then rule id, then entity — so the same install always renders the
/// same list.
/// </summary>
public sealed class OperationsHealthEngine
{
    private readonly IReadOnlyList<IOperationsRule> _rules;

    public OperationsHealthEngine()
        : this([new FailingTaskRule(), new StaleTaskRule(), new BrokenActivityLinkRule(), new LogGrowthRule(), new TableBloatRule()])
    {
    }

    public OperationsHealthEngine(IReadOnlyList<IOperationsRule> rules) => _rules = rules;

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
