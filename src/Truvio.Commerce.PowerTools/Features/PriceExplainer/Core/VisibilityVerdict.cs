namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

public sealed class VisibilityVerdict
{
    public VisibilityOutcome Outcome { get; init; }
    public bool Visible => Outcome is not VisibilityOutcome.Hidden;
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<AssortmentRowVerdict> Rows { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
