namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

public sealed record IndexSpec(
    string Repository,
    string Item,
    string Name,
    string Balancer,
    string BuilderType,
    IReadOnlyList<IndexFieldSpec> Fields,
    IReadOnlyList<IndexInstanceSpec> Instances,
    IReadOnlyList<IndexBuildSpec> Builds,
    IndexHealth Health,
    string HealthDetail,
    DateTime? LastBuild,
    string OnlineInstance)
{
    /// <summary>Repository-qualified item name; the identity a query's source points at.</summary>
    public string Key => SearchKeys.For(Repository, Item);

    public IndexFieldSpec? Field(string? systemName) =>
        string.IsNullOrEmpty(systemName)
            ? null
            : Fields.FirstOrDefault(f => string.Equals(f.SystemName, systemName, StringComparison.Ordinal));
}
