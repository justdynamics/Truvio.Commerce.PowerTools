using System.Collections;
using System.Globalization;
using Dynamicweb.Indexing;
using Dynamicweb.Indexing.Queries;
using Dynamicweb.Indexing.Querying;
using Dynamicweb.Indexing.Querying.Expressions;
using DwQuery = Dynamicweb.Indexing.Querying.Query;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Dw;

/// <summary>How an indexed product document compares with the product row in the database.</summary>
public enum ProductMatch
{
    NotAProduct,
    Match,
    Differs,
    MissingInDatabase,
    Unknown
}
