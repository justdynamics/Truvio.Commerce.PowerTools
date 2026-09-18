namespace Truvio.Commerce.PowerTools.Features.QueryTester.Core;

/// <summary>
/// One node of the expression tree, resolved for a concrete run: what it compares, what the
/// value turned out to be, and whether it survives to the index provider.
/// </summary>
public sealed record ClauseTrace(
    string Path,
    int Depth,
    bool IsGroup,
    string Label,
    string Field,
    string Operator,
    string ParameterName,
    ValueOrigin Origin,
    string ResolvedValue,
    ClauseVerdict Verdict,
    string Explanation)
{
    /// <summary>Only clause rows can be toggled off to measure their impact.</summary>
    public bool IsMeasurable => !IsGroup && Verdict == ClauseVerdict.Active;

    public string VerdictText => Verdict switch
    {
        ClauseVerdict.Active => "Active",
        ClauseVerdict.Disabled => "Disabled",
        ClauseVerdict.Dropped => "Dropped",
        ClauseVerdict.Throws => "Throws",
        ClauseVerdict.UnknownField => "Unknown field",
        ClauseVerdict.MatchesEverything => "Always true",
        _ => Verdict.ToString()
    };

    /// <summary>Colour bucket for the report tables.</summary>
    public string VerdictKind => Verdict switch
    {
        ClauseVerdict.Active => "ok",
        ClauseVerdict.Disabled => "warn",
        ClauseVerdict.Dropped => "warn",
        ClauseVerdict.Throws => "bad",
        ClauseVerdict.UnknownField => "bad",
        ClauseVerdict.MatchesEverything => "bad",
        _ => "info"
    };
}
