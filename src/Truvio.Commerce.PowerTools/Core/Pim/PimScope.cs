namespace Truvio.Commerce.PowerTools.Core.Pim;

/// <summary>
/// The shipped scan cap, in its own type because a record's positional default cannot
/// reference a constant declared on the record itself.
/// </summary>
public static class PimScopeDefaults
{
    public const int ProductCap = 200;
}

/// <summary>
/// What the toolbar pickers set: which slice of the catalog a scan covers, and how far it is
/// allowed to go. Every screen in the section round-trips this through its query, so a scoped
/// report is always a shareable URL.
/// </summary>
/// <param name="GroupId">Product group to scan; empty = the whole catalog.</param>
/// <param name="LanguageId">Language the scores are read in; empty = DW's default language.</param>
/// <param name="ProductCap">
/// Most products scored in one scan. The bulk completeness call is the expensive part of every
/// screen here, so the cap is the difference between a report and a timeout.
/// </param>
/// <param name="Search">Free text narrowing the product enumeration (number, name).</param>
public sealed record PimScope(
    string GroupId = "",
    string LanguageId = "",
    int ProductCap = PimScopeDefaults.ProductCap,
    string Search = "")
{
    public const int DefaultProductCap = PimScopeDefaults.ProductCap;

    public static PimScope Default => new();

    /// <summary>A cap of zero or less means "unset" — fall back to the shipped default.</summary>
    public int EffectiveCap => ProductCap > 0 ? ProductCap : DefaultProductCap;

    public bool HasGroup => !string.IsNullOrEmpty(GroupId);

    public bool HasSearch => !string.IsNullOrWhiteSpace(Search);
}
