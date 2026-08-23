namespace Truvio.Commerce.PowerApps.SecOps.Core.Diagnostics;

public enum FindingSeverity
{
    Info,
    Warning,
    Critical
}

/// <summary>One misconfiguration surfaced by a warning rule.</summary>
public sealed record Finding(
    string RuleId,
    FindingSeverity Severity,
    string EntityName,
    string EntityKey,
    string EntityDisplayName,
    string Title,
    string Detail);
