using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Core;

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
