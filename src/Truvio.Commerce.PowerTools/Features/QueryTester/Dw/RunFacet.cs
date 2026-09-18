namespace Truvio.Commerce.PowerTools.Features.QueryTester.Dw;

/// <summary>Facet buckets for one facet of one facet group.</summary>
public sealed record RunFacet(string Group, string Facet, string Field, IReadOnlyList<KeyValuePair<string, long>> Values);
