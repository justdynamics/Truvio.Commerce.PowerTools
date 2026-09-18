namespace Truvio.Commerce.PowerTools.Shared.Currency;

/// <summary>
/// One explicit-currency price-matrix row, used to derive what the exchange rates *should*
/// be from the solution's own data. Rows whose currency column is blank are wildcards — DW
/// applies their amount unconverted in whatever currency is asked — so they carry no exchange
/// information and must not be sampled.
/// </summary>
public sealed record CurrencyPriceSample(
    string ProductId,
    string VariantId,
    string CurrencyCode,
    double Quantity,
    double Amount,
    bool IsWithVat);
