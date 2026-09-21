namespace Truvio.Commerce.PowerTools.Features.PimQuality.Core;

/// <summary>A workflow referenced by groups or products — the governance screen's rows.</summary>
public sealed record WorkflowUsage(string Name, bool UsedByGroups, bool UsedByProducts)
{
    public bool IsReferenced => UsedByGroups || UsedByProducts;
}
