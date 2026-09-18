using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Models;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Models;

/// <summary>A heading plus rendered rows — one block on a PIM report screen.</summary>
public sealed class PimSectionModel
{
    public string Heading { get; set; } = string.Empty;

    public List<OpsRowModel> Rows { get; set; } = [];
}
