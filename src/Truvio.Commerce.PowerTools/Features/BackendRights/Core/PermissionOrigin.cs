namespace Truvio.Commerce.PowerTools.Features.BackendRights.Core;

/// <summary>How a permission level was arrived at, for the explanation.</summary>
public enum PermissionOrigin
{
    Explicit,
    Inherited,
    RoleDefault,
    ContextDefault,
    NotEvaluated
}
