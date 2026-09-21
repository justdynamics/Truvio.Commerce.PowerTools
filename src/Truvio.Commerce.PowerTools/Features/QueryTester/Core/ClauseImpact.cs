namespace Truvio.Commerce.PowerTools.Features.QueryTester.Core;

/// <summary>Hit counts measured for one clause by re-running the query without it.</summary>
public sealed record ClauseImpact(
    string Path,
    string Label,
    int? WithoutClause,
    int? ClauseAlone)
{
    public bool KillsResult => ClauseAlone is 0;
}
