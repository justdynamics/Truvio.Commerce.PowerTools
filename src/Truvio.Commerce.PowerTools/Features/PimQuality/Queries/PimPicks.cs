using System.Globalization;
using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Queries;

/// <summary>Sentinel ids used by the PIM toolbar pickers.</summary>
internal static class PimPicks
{
    /// <summary>The "no group filter" entry — an empty id cannot round-trip through the picker.</summary>
    public const string WholeCatalog = "__all__";
}
