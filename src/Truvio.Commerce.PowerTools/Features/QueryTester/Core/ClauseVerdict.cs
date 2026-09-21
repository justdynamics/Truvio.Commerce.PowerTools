namespace Truvio.Commerce.PowerTools.Features.QueryTester.Core;

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
