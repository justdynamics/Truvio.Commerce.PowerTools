using Dynamicweb.Data;
using Dynamicweb.Ecommerce;

namespace Truvio.Commerce.PowerTools.Core.Commerce.Dw;

/// <summary>
/// Reads what <see cref="CurrencyGuard"/> evaluates: the configured currencies and a capped
/// sample of explicit-currency price-matrix rows. Strictly read-only.
/// </summary>
public static class DwCurrencyReader
{
    /// <summary>Most price rows sampled for the implied-rate consistency check (CUR-W2).</summary>
    public const int SampleCap = 5000;

    public static IReadOnlyList<CurrencySpec> GetCurrencies()
    {
        try
        {
            return Services.Currencies.GetAllCurrencies()
                .Select(c => new CurrencySpec(
                    c.Code,
                    c.GetName(Services.Languages.GetDefaultLanguageId()) is { Length: > 0 } name ? name : c.Code,
                    c.Rate,
                    c.IsDefault))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Explicit-currency, non-informative price rows. Wildcard-currency rows are excluded at
    /// the source: DW applies their amount unconverted in whatever currency is asked, so they
    /// carry no exchange information.
    /// </summary>
    public static IReadOnlyList<CurrencyPriceSample> GetPriceSamples(int cap = SampleCap)
    {
        var samples = new List<CurrencyPriceSample>();
        try
        {
            var sql = CommandBuilder.Create(
                """
                SELECT TOP ({0}) PriceProductId, PriceProductVariantID, PriceCurrency,
                       PriceQuantity, PriceAmount, PriceIsWithVat
                FROM EcomPrices
                WHERE PriceCurrency IS NOT NULL AND PriceCurrency <> '' AND PriceIsInformative = 0
                ORDER BY PriceProductId, PriceProductVariantID, PriceQuantity
                """,
                cap);

            using var reader = Database.CreateDataReader(sql);
            while (reader.Read())
            {
                samples.Add(new CurrencyPriceSample(
                    Convert.ToString(reader["PriceProductId"]) ?? string.Empty,
                    Convert.ToString(reader["PriceProductVariantID"]) ?? string.Empty,
                    Convert.ToString(reader["PriceCurrency"]) ?? string.Empty,
                    Convert.ToDouble(reader["PriceQuantity"]),
                    Convert.ToDouble(reader["PriceAmount"]),
                    Convert.ToBoolean(reader["PriceIsWithVat"])));
            }
        }
        catch
        {
            return [];
        }

        return samples;
    }
}
