namespace Truvio.Commerce.PowerTools.Features.QueryTester.Dw;

/// <summary>The outcome of one execution of a repository query.</summary>
public sealed record QueryRunResult(
    int TotalHits,
    int Returned,
    double ElapsedMs,
    string Instance,
    string LuceneQuery,
    string Error,
    IReadOnlyList<RunDocument> Documents,
    IReadOnlyList<RunFacet> Facets)
{
    public static QueryRunResult Failed(string error) => new(0, 0, 0, string.Empty, string.Empty, error, [], []);

    public bool Ok => string.IsNullOrEmpty(Error);
}
