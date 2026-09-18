namespace Truvio.Commerce.PowerTools.Features.BackendRights.Core;

/// <summary>
/// One row of <c>CapabilityLimitation</c>: a DENY, keyed by user group. No row means allowed, and
/// there are no per-user or per-role rows.
/// </summary>
public sealed record CapabilityLimitationSpec(int UserGroupId, string GroupName, string Key)
{
    /// <summary>True when the group id no longer resolves to a live group.</summary>
    public bool GroupMissing => string.IsNullOrEmpty(GroupName);
}
