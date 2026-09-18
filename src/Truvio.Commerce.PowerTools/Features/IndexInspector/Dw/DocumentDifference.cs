using System.Collections;
using System.Globalization;
using Dynamicweb.Indexing;
using Dynamicweb.Indexing.Queries;
using Dynamicweb.Indexing.Querying;
using Dynamicweb.Indexing.Querying.Expressions;
using DwQuery = Dynamicweb.Indexing.Querying.Query;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Dw;

public sealed record DocumentDifference(string Field, string InIndex, string InDatabase);
