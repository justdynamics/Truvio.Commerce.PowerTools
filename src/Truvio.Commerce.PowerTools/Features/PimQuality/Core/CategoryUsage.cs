namespace Truvio.Commerce.PowerTools.Features.PimQuality.Core;

/// <summary>A product category and how many groups use it; zero = nothing references it.</summary>
public sealed record CategoryUsage(string CategoryId, string Name, int GroupCount)
{
    public bool IsUnused => GroupCount == 0;
}
