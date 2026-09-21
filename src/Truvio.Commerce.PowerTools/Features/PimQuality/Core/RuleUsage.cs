namespace Truvio.Commerce.PowerTools.Features.PimQuality.Core;

/// <summary>A completion rule and everything that references it; no references = dead config.</summary>
/// <param name="Usages">Human-readable "Shop 'Northwind'" / "Group 'Engines'" strings.</param>
public sealed record RuleUsage(
    int RuleId,
    string Name,
    IReadOnlyList<string> FieldSystemNames,
    bool ExcludeVariants,
    IReadOnlyList<string> Usages)
{
    public bool IsDead => Usages.Count == 0;
}
