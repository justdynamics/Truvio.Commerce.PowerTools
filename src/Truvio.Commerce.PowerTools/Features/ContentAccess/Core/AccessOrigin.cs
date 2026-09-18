namespace Truvio.Commerce.PowerTools.Features.ContentAccess.Core;

public enum AccessOrigin
{
    /// <summary>Account bypasses all permission checks (Angel / built-in admin / Administrator).</summary>
    Bypass,

    /// <summary>An explicit row on the entity itself decided the level.</summary>
    ExplicitHere,

    /// <summary>An explicit row on an ancestor page decided the level (page inheritance).</summary>
    InheritedFromPage,

    /// <summary>No explicit row applied to any of the account's identities; a frontend role default won.</summary>
    RoleDefault,

    /// <summary>Grid row / paragraph carries no rows of its own; the page outcome applies.</summary>
    PageFallback
}
