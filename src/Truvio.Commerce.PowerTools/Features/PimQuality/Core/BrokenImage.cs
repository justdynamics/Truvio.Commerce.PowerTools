namespace Truvio.Commerce.PowerTools.Features.PimQuality.Core;

/// <summary>A product whose resolved image path has no file behind it.</summary>
public sealed record BrokenImage(
    string ProductId,
    string Number,
    string Name,
    string Path);
