using Dynamicweb.Data;
using Dynamicweb.Security.Permissions;
using Dynamicweb.Security.UserManagement;

namespace Truvio.Commerce.PowerTools.Core.Permissions.Dw;

/// <summary>
/// <see cref="IContentSecuritySource"/> backed by the live DW runtime. Strictly read-only:
/// structure through the content services, permission rows through
/// <see cref="PermissionService.GetPermissionsByQuery"/>, and the legacy permission-column
/// remnants through a read-only SQL probe (the DW10 API no longer exposes those columns).
/// </summary>
public sealed class DwContentSecuritySource : IContentSecuritySource
{
    public IReadOnlyList<AreaNode> GetAreas() =>
        Dynamicweb.Content.Services.Areas.GetAreas()
            .Select(a => new AreaNode(a.ID, string.IsNullOrEmpty(a.DisplayName) ? a.Name : a.DisplayName))
            .ToList();

    public IReadOnlyList<PageNode> GetPages(int areaId) =>
        Dynamicweb.Content.Services.Pages.GetPagesByAreaID(areaId)
            .Select(p => new PageNode(p.ID, p.ParentPageId, p.AreaId, p.GetDisplayName(), p.Sort, p.Active, p.Hidden))
            .ToList();

    public IReadOnlyList<GridRowNode> GetGridRows(int pageId) =>
        Dynamicweb.Content.Services.Grids.GetGridRowsByPageId(pageId)
            .Select(g => new GridRowNode(g.ID, g.PageId, string.IsNullOrEmpty(g.TemplateName) ? $"Row {g.ID}" : g.TemplateName))
            .ToList();

    public IReadOnlyList<ParagraphNode> GetParagraphs(int pageId) =>
        Dynamicweb.Content.Services.Paragraphs.GetParagraphsByPageId(pageId)
            .Select(p => new ParagraphNode(
                p.ID,
                p.PageID,
                string.IsNullOrEmpty(p.Header) ? $"Paragraph {p.ID}" : p.Header,
                p.ModuleSystemName ?? string.Empty))
            .ToList();

    public IReadOnlyList<ContentPermissionRow> GetRows(string entityName) =>
        new PermissionService()
            .GetPermissionsByQuery(new PermissionQuery { Name = entityName })
            .Select(p => new ContentPermissionRow(p.OwnerId, p.Name, p.Key, (int)p.Level))
            .ToList();

    public IReadOnlySet<string> GetExistingGroupIds() =>
        UserManagementServices.UserGroups.GetGroups()
            .Select(g => g.ID.ToString())
            .ToHashSet(StringComparer.Ordinal);

    public IReadOnlyList<int> GetPagesWithLegacyPermissionValues() =>
        ReadLegacyIds("Page", "PageID", "PagePermission");

    public IReadOnlyList<int> GetParagraphsWithLegacyPermissionValues() =>
        ReadLegacyIds("Paragraph", "ParagraphID", "ParagraphPermission");

    /// <summary>
    /// Reads the ids whose legacy permission column is populated. The column is checked in
    /// INFORMATION_SCHEMA first: a schema without it yields no findings instead of a failed
    /// query, because DW logs every failed SQL statement to the event log before throwing.
    /// </summary>
    private static List<int> ReadLegacyIds(string table, string idColumn, string permissionColumn)
    {
        if (!ColumnExists(table, permissionColumn))
            return new List<int>();
        return ReadIds($"SELECT [{idColumn}] FROM [{table}] WHERE [{permissionColumn}] IS NOT NULL AND [{permissionColumn}] <> ''");
    }

    private static bool ColumnExists(string table, string column)
    {
        try
        {
            var sql = CommandBuilder.Create(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = {0} AND COLUMN_NAME = {1}",
                table,
                column);
            return Convert.ToInt32(Database.ExecuteScalar(sql)) > 0;
        }
        catch
        {
            return false;
        }
    }

    private static List<int> ReadIds(string sql)
    {
        var ids = new List<int>();
        try
        {
            using var reader = Database.CreateDataReader(sql);
            while (reader.Read())
                ids.Add(Convert.ToInt32(reader[0]));
        }
        catch
        {
            // Defensive only: the column existence check above keeps this from being reached on a known schema.
        }
        return ids;
    }
}
