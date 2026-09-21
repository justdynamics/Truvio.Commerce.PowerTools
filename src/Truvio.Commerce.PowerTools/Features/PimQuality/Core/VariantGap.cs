namespace Truvio.Commerce.PowerTools.Features.PimQuality.Core;

/// <summary>
/// A product whose variant groups allow more combinations than actually exist.
/// <para>
/// <paramref name="PotentialCount"/> is DW's own count and can be astronomically large; when it
/// exceeds what is safe to enumerate, <paramref name="MissingExamples"/> stays empty and only
/// the number is reported.
/// </para>
/// </summary>
public sealed record VariantGap(
    string ProductId,
    string Number,
    string Name,
    ulong PotentialCount,
    int ExistingCount,
    IReadOnlyList<string> MissingExamples)
{
    /// <summary>Combinations the catalog does not have. Never negative — DW's potential count is a ceiling.</summary>
    public ulong MissingCount =>
        PotentialCount > (ulong)ExistingCount ? PotentialCount - (ulong)ExistingCount : 0;

    public bool HasGap => MissingCount > 0;
}
