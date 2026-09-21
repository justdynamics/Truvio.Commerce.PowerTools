namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

/// <summary>One assortment, reduced to what decides product visibility for an account.</summary>
public sealed record AssortmentSpec
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool Active { get; init; }
    public bool AllowAnonymousUsers { get; init; }
    /// <summary>True when the (built) assortment contains the product/variant under inspection.</summary>
    public bool ContainsProduct { get; init; }
    /// <summary>True when the assortment is flagged for rebuild — its item list may be stale.</summary>
    public bool RebuildRequired { get; init; }
    public IReadOnlySet<int> PermittedUserIds { get; init; } = new HashSet<int>();
    public IReadOnlySet<int> PermittedGroupIds { get; init; } = new HashSet<int>();
}
