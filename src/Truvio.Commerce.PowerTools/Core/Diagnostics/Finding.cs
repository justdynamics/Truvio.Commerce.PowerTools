namespace Truvio.Commerce.PowerTools.Core.Diagnostics;

public enum FindingSeverity
{
    Info,
    Warning,
    Critical
}

/// <summary>
/// One misconfiguration surfaced by a warning rule.
/// <para>
/// <paramref name="Subject"/> is optional and names what the finding is *about* beyond the
/// entity — currently the comma-separated query parameter names behind an IDX-W1/IDX-W2
/// finding. It lets the settings layer suppress "findings about parameter X" precisely,
/// instead of pattern-matching the human-readable title.
/// </para>
/// </summary>
public sealed record Finding(
    string RuleId,
    FindingSeverity Severity,
    string EntityName,
    string EntityKey,
    string EntityDisplayName,
    string Title,
    string Detail,
    string? Subject = null);
