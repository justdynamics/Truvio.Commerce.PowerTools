namespace Truvio.Commerce.PowerApps.SecOps.Core.Permissions;

/// <summary>
/// Read-only access to everything the Security Viewer evaluates: the content structure
/// (areas, pages, grid rows, paragraphs), the render-time permission rows, and the legacy
/// permission-column remnants. Implemented against the DW runtime by
/// <see cref="Dw.DwContentSecuritySource"/> and by an in-memory fake in tests.
/// </summary>
public interface IContentSecuritySource
{
    IReadOnlyList<AreaNode> GetAreas();

    IReadOnlyList<PageNode> GetPages(int areaId);

    IReadOnlyList<GridRowNode> GetGridRows(int pageId);

    IReadOnlyList<ParagraphNode> GetParagraphs(int pageId);

    /// <summary>All render-time rows for one entity name ("Page", "GridRow", "Paragraph").</summary>
    IReadOnlyList<ContentPermissionRow> GetRows(string entityName);

    /// <summary>Ids of every user group that exists (used to detect orphaned grants).</summary>
    IReadOnlySet<string> GetExistingGroupIds();

    /// <summary>
    /// Page ids whose legacy Page.PagePermission column is non-empty. The DW10 runtime
    /// ignores that column, so a populated value signals a false sense of gating.
    /// </summary>
    IReadOnlyList<int> GetPagesWithLegacyPermissionValues();

    /// <summary>
    /// Paragraph ids whose legacy EcomParagraph.ParagraphPermission column is non-empty.
    /// </summary>
    IReadOnlyList<int> GetParagraphsWithLegacyPermissionValues();
}
