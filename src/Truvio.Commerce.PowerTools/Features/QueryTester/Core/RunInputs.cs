using Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

namespace Truvio.Commerce.PowerTools.Features.QueryTester.Core;

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
