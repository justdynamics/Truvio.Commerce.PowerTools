using System.Globalization;
using Dynamicweb.Extensibility;
using Dynamicweb.Indexing;
using Dynamicweb.Indexing.Queries;
using Dynamicweb.Indexing.Querying;
using Dynamicweb.Indexing.Querying.Expressions;
using Dynamicweb.Indexing.Querying.Faceting;
using DwExpression = Dynamicweb.Indexing.Querying.Expressions.Expression;
using DwQuery = Dynamicweb.Indexing.Querying.Query;

namespace Truvio.Commerce.PowerTools.Features.QueryTester.Dw;

/// <summary>Facet buckets for one facet of one facet group.</summary>
public sealed record RunFacet(string Group, string Facet, string Field, IReadOnlyList<KeyValuePair<string, long>> Values);
