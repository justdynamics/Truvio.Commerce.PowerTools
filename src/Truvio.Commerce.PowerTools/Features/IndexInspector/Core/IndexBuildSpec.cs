namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

public sealed record IndexBuildSpec(
    string Name,
    string Action,
    string BuilderType,
    IReadOnlyList<KeyValuePair<string, string>> Settings);
