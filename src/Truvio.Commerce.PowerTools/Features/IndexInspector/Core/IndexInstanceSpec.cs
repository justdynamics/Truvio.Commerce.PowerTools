namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

public sealed record IndexInstanceSpec(
    string Name,
    string ProviderType,
    bool IsOnline,
    bool IsAvailable,
    string State,
    DateTime? LastBuild,
    TimeSpan? Duration);
