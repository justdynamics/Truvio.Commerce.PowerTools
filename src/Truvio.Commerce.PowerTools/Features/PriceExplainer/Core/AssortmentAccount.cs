namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

/// <summary>The account the visibility is evaluated for (null user = anonymous).</summary>
public sealed record AssortmentAccount
{
    public int? UserId { get; init; }
    public IReadOnlySet<int> GroupIds { get; init; } = new HashSet<int>();
    public bool IsAnonymous => UserId is null;
}
