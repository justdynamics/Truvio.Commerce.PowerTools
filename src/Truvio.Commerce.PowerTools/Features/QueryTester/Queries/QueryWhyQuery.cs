using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.Features.QueryTester.Models;
using Truvio.Commerce.PowerTools.Shared.AdminUI;

namespace Truvio.Commerce.PowerTools.Features.QueryTester.Queries;

/// <summary>Feeds the "Why 'X'?" slide-over: one document of one query run, explained.</summary>
public sealed class QueryWhyQuery : DataQueryModelBase<QueryWhyModel>
{
    public string Repository { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    /// <summary>The run's values, as <c>name=value;...</c> — the panel probes with them.</summary>
    public string Parameters { get; set; } = string.Empty;

    /// <summary>The document key to explain.</summary>
    public string Key { get; set; } = string.Empty;

    public override QueryWhyModel? GetModel()
    {
        if (string.IsNullOrEmpty(Repository) || string.IsNullOrEmpty(Item) || string.IsNullOrEmpty(Key))
            return new QueryWhyModel { Heading = "Why?", Html = SearchTables.Note("No document selected.") };

        var why = WhyReport.Build(Repository, Item, Parameters, Key, compact: true);
        return new QueryWhyModel { Heading = why.Heading, Html = why.Html };
    }
}
