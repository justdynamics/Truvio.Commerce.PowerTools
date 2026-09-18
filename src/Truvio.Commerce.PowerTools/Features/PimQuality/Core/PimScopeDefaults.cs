namespace Truvio.Commerce.PowerTools.Features.PimQuality.Core;

/// <summary>
/// The shipped scan cap, in its own type because a record's positional default cannot
/// reference a constant declared on the record itself.
/// </summary>
public static class PimScopeDefaults
{
    public const int ProductCap = 200;
}
