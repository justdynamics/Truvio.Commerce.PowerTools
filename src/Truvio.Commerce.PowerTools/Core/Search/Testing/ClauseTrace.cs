namespace Truvio.Commerce.PowerTools.Core.Search.Testing;

/// <summary>What happens to one clause when the query is executed with a given set of values.</summary>
public enum ClauseVerdict
{
    /// <summary>The clause reaches the index provider and constrains the result.</summary>
    Active,

    /// <summary>Left disabled in the query editor — the provider returns null for it.</summary>
    Disabled,

    /// <summary>Its value resolves to null, so the provider silently removes the clause.</summary>
    Dropped,

    /// <summary>
    /// Its field is not in the index schema and this platform version throws
    /// <c>ArgumentException</c> for that — the WHOLE query fails (Dynamicweb up to 10.19).
    /// </summary>
    Throws,

    /// <summary>
    /// Its field is not in the index schema and this platform version only logs a warning and
    /// removes the clause (Dynamicweb from 10.21) — the query still runs, but wider.
    /// </summary>
    UnknownField,

    /// <summary>Nothing is left of the whole expression, so the provider matches every document.</summary>
    MatchesEverything
}

/// <summary>Where the value on the right-hand side of a clause came from for this run.</summary>
public enum ValueOrigin
{
    None,
    SuppliedValue,
    ParameterDefault,
    MissingParameter,
    UndeclaredParameter,
    Constant,
    Term,
    Macro,
    Code,
    FullText
}

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

/// <summary>A concrete edit the user can make, ranked by <see cref="Kind"/>.</summary>
public sealed record Suggestion(string Kind, string Title, string Detail)
{
    public static Suggestion Fix(string title, string detail) => new("fix", title, detail);

    public static Suggestion Warn(string title, string detail) => new("warn", title, detail);

    public static Suggestion Info(string title, string detail) => new("info", title, detail);

    /// <summary>Sort weight — problems that break the result come first.</summary>
    public int Rank => Kind switch { "fix" => 0, "warn" => 1, _ => 2 };
}

/// <summary>
/// Everything the pure diagnosis needs: the query and its index, the values the user
/// supplied, the values the live adapter resolved for macro/code expressions (keyed by
/// clause path — the diagnosis itself never talks to Dynamicweb), and how the host platform
/// reacts to a clause field that is not in the schema.
/// <para>
/// <c>DropsUnknownField</c> is false for the platform behaviour at the 10.8.4 floor
/// (<c>Helpers.ParseQueryExpressionInternal</c> throws <c>ArgumentException</c> and the whole
/// query fails) and true from 10.21 onwards, where the same code path logs a warning through
/// <c>LogManager.System.GetLogger("Provider", "LuceneIndexProvider")</c> and returns null for
/// the clause instead.
/// </para>
/// </summary>
public sealed record RunInputs(
    QuerySpec Query,
    IndexSpec? Index,
    IReadOnlyDictionary<string, string> Values,
    IReadOnlyDictionary<string, string> RuntimeValues,
    bool DropsUnknownField = false)
{
    public static RunInputs For(QuerySpec query, IndexSpec? index, string? parameterText, bool dropsUnknownField = false) =>
        new(query, index, ParameterValues.Effective(parameterText),
            new Dictionary<string, string>(StringComparer.Ordinal), dropsUnknownField);

    public RunInputs WithRuntimeValues(IReadOnlyDictionary<string, string> runtimeValues) =>
        this with { RuntimeValues = runtimeValues };
}

/// <summary>Hit counts measured for one clause by re-running the query without it.</summary>
public sealed record ClauseImpact(
    string Path,
    string Label,
    int? WithoutClause,
    int? ClauseAlone)
{
    public bool KillsResult => ClauseAlone is 0;
}

/// <summary>How one document behaves against one clause — the "why doesn't PROD27 show up" row.</summary>
public sealed record ExpectationCheck(
    string Path,
    string Label,
    string Field,
    string DocumentValue,
    bool Passes,
    string Note);
