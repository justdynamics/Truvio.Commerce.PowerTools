using System.Collections;
using System.Globalization;
using Dynamicweb.Indexing;
using Dynamicweb.Indexing.Queries;
using Dynamicweb.Indexing.Querying;
using Dynamicweb.Indexing.Querying.Expressions;
using DwQuery = Dynamicweb.Indexing.Querying.Query;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Dw;

/// <summary>One field of an index document, already rendered for display.</summary>
public sealed record DocumentField(string Name, string Value);
