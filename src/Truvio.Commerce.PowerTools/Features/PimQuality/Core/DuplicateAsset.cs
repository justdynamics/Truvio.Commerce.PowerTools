namespace Truvio.Commerce.PowerTools.Features.PimQuality.Core;

/// <summary>The same asset path attached to one product more than once.</summary>
public sealed record DuplicateAsset(
    string ProductId,
    string Number,
    string Name,
    string Path,
    int Count);
