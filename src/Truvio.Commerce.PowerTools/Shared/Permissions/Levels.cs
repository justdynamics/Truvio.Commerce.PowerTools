namespace Truvio.Commerce.PowerTools.Shared.Permissions;

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
