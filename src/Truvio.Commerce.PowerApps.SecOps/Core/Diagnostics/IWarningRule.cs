using Truvio.Commerce.PowerApps.SecOps.Core.Permissions;

namespace Truvio.Commerce.PowerApps.SecOps.Core.Diagnostics;

/// <summary>
/// A single misconfiguration detector. Rules are pure over the source snapshot so they can
/// run against an in-memory fake in tests.
/// </summary>
public interface IWarningRule
{
    string RuleId { get; }

    IEnumerable<Finding> Evaluate(WarningContext context);
}

/// <summary>
/// Shared, lazily-materialized lookups over the source so each rule doesn't re-walk the
/// content structure.
/// </summary>
public sealed class WarningContext
{
    private readonly Lazy<IReadOnlyDictionary<int, PageNode>> _pagesById;
    private readonly Lazy<IReadOnlyDictionary<int, GridRowNode>> _gridRowsById;
    private readonly Lazy<IReadOnlyDictionary<int, ParagraphNode>> _paragraphsById;

    public WarningContext(IContentSecuritySource source)
    {
        Source = source;
        _pagesById = new(() => source.GetAreas()
            .SelectMany(a => source.GetPages(a.Id))
            .ToDictionary(p => p.Id));
        _gridRowsById = new(() => PagesById.Value.Keys
            .SelectMany(source.GetGridRows)
            .ToDictionary(g => g.Id));
        _paragraphsById = new(() => PagesById.Value.Keys
            .SelectMany(source.GetParagraphs)
            .ToDictionary(p => p.Id));
    }

    public IContentSecuritySource Source { get; }

    public Lazy<IReadOnlyDictionary<int, PageNode>> PagesById => _pagesById;

    public Lazy<IReadOnlyDictionary<int, GridRowNode>> GridRowsById => _gridRowsById;

    public Lazy<IReadOnlyDictionary<int, ParagraphNode>> ParagraphsById => _paragraphsById;

    public string DescribeEntity(string entityName, string key)
    {
        if (!int.TryParse(key, out var id))
            return $"{entityName} {key}";

        return entityName switch
        {
            ContentEntityNames.Page when PagesById.Value.TryGetValue(id, out var p) => $"Page '{p.Name}' ({id})",
            ContentEntityNames.GridRow when GridRowsById.Value.TryGetValue(id, out var g) =>
                $"Row '{g.Name}' ({id}) on {DescribeEntity(ContentEntityNames.Page, g.PageId.ToString())}",
            ContentEntityNames.Paragraph when ParagraphsById.Value.TryGetValue(id, out var p) =>
                $"Paragraph '{p.Name}' ({id}) on {DescribeEntity(ContentEntityNames.Page, p.PageId.ToString())}",
            _ => $"{entityName} {key}"
        };
    }
}
