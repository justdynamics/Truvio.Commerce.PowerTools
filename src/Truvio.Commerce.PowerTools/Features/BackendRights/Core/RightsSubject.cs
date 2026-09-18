namespace Truvio.Commerce.PowerTools.Features.BackendRights.Core;

/// <summary>
/// The backend user the report is about. <paramref name="AllowBackend"/> is
/// <c>GetAllowBackendWithInheritance()</c> — false means the user cannot reach the admin at all,
/// whatever else is granted.
/// </summary>
public sealed record RightsSubject(
    int UserId,
    string DisplayName,
    string UserName,
    bool AllowBackend,
    bool IsAdmin,
    bool IsAngel,
    bool IsBuiltInAdmin)
{
    /// <summary>Angel and built-in admin skip BOTH gates; the Administrator user type skips only permissions.</summary>
    public bool IsElevated => IsAngel || IsBuiltInAdmin;

    public string StatusName =>
        !AllowBackend ? "No access"
        : IsElevated ? "Elevated"
        : IsAdmin ? "Administrator"
        : "Standard";
}
