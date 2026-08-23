namespace Truvio.Commerce.PowerTools.Core.Permissions;

/// <summary>Entity names used by DW's render-time content permission rows.</summary>
public static class ContentEntityNames
{
    public const string Page = "Page";
    public const string GridRow = "GridRow";
    public const string Paragraph = "Paragraph";
}

/// <summary>
/// One UnifiedPermission row on a content entity. OwnerId is a built-in role name
/// ("Anonymous", "AuthenticatedFrontend", ...) or a numeric user-group id.
/// </summary>
public sealed record ContentPermissionRow(string OwnerId, string EntityName, string Key, int Level);

/// <summary>PermissionLevel bit values (Dynamicweb.Security.Permissions.PermissionLevel).</summary>
public static class Levels
{
    public const int NotSet = 0;
    public const int None = 1;
    public const int Read = 4;
    public const int Edit = 20;
    public const int Create = 84;
    public const int Delete = 340;
    public const int All = 1364;

    public static bool GrantsRead(int level) => (level & Read) == Read;

    public static string Name(int level) => level switch
    {
        NotSet => "Not set",
        None => "None",
        Read => "Read",
        Edit => "Edit",
        Create => "Create",
        Delete => "Delete",
        All => "All",
        _ => $"Level {level}"
    };
}

/// <summary>Minimal page-tree node the evaluator walks. Mirrors Dynamicweb.Content.Page.</summary>
public sealed record PageNode(int Id, int ParentPageId, int AreaId, string Name, int Sort, bool Active, bool Hidden);

public sealed record AreaNode(int Id, string Name);

public sealed record GridRowNode(int Id, int PageId, string Name);

public sealed record ParagraphNode(int Id, int PageId, string Name, string ModuleSystemName);
