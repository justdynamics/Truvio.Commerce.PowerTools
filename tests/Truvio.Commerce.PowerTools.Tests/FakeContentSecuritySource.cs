using Truvio.Commerce.PowerTools.Core.Permissions;

namespace Truvio.Commerce.PowerTools.Tests;

/// <summary>In-memory <see cref="IContentSecuritySource"/> for evaluator and rule tests.</summary>
public sealed class FakeContentSecuritySource : IContentSecuritySource
{
    public List<AreaNode> Areas { get; } = [];
    public List<PageNode> Pages { get; } = [];
    public List<GridRowNode> GridRows { get; } = [];
    public List<ParagraphNode> Paragraphs { get; } = [];
    public List<ContentPermissionRow> Rows { get; } = [];
    public HashSet<string> GroupIds { get; } = new(StringComparer.Ordinal);
    public List<int> LegacyPageIds { get; } = [];
    public List<int> LegacyParagraphIds { get; } = [];

    public IReadOnlyList<AreaNode> GetAreas() => Areas;

    public IReadOnlyList<PageNode> GetPages(int areaId) => Pages.Where(p => p.AreaId == areaId).ToList();

    public IReadOnlyList<GridRowNode> GetGridRows(int pageId) => GridRows.Where(g => g.PageId == pageId).ToList();

    public IReadOnlyList<ParagraphNode> GetParagraphs(int pageId) => Paragraphs.Where(p => p.PageId == pageId).ToList();

    public IReadOnlyList<ContentPermissionRow> GetRows(string entityName) =>
        Rows.Where(r => r.EntityName == entityName).ToList();

    public IReadOnlySet<string> GetExistingGroupIds() => GroupIds;

    public IReadOnlyList<int> GetPagesWithLegacyPermissionValues() => LegacyPageIds;

    public IReadOnlyList<int> GetParagraphsWithLegacyPermissionValues() => LegacyParagraphIds;
}
