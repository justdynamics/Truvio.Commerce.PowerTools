using System.Globalization;
using Truvio.Commerce.PowerTools.Core.Diagnostics;

namespace Truvio.Commerce.PowerTools.Core.Commerce;

/// <summary>One currency as configured in DW, reduced to what the conversion math uses.</summary>
public sealed record CurrencySpec(string Code, string Name, double Rate, bool IsDefault);

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

/// <summary>
/// How the price the report shows was converted out of the default currency — the factor, the
/// formula behind it, and anything structurally wrong with the rates involved.
/// </summary>
public sealed record ConversionExplanation(
    string FromCode,
    string ToCode,
    double Factor,
    string ImpliedRateText,
    string FormulaText,
    IReadOnlyList<Finding> Findings)
{
    public bool Broken => Findings.Any(f => f.Severity == FindingSeverity.Critical);
}

/// <summary>
/// The currency sanity rules, pure and host-free.
/// <para>
/// DW's conversion is <c>PriceCalculator.Exchange(price, sourceRate, destinationRate) =
/// sourceRate / destinationRate × price</c> (verified 10.25 decompile). The convention that
/// makes this correct: a currency's <c>Rate</c> is the value of 100 units of that currency
/// expressed in the default currency, and the default currency's own Rate is exactly 100. A
/// default whose Rate is anything else scales <b>every</b> converted price by Rate/100 — the
/// classic symptom is "all non-default prices are 100× too low" from a default seeded with
/// Rate = 1.
/// </para>
/// </summary>
public static class CurrencyGuard
{
    /// <summary>The default currency's rate is not 100 — every conversion is scaled by Rate/100.</summary>
    public const string DefaultRateId = "CUR-C1";

    /// <summary>A currency rate is zero or negative — DW divides by it.</summary>
    public const string NonPositiveRateId = "CUR-C2";

    /// <summary>No default currency exists.</summary>
    public const string NoDefaultId = "CUR-C3";

    /// <summary>A non-default currency still has DW's seed rate of exactly 100.</summary>
    public const string UntouchedRateId = "CUR-W1";

    /// <summary>The configured rate contradicts what the solution's own price rows imply.</summary>
    public const string MatrixDeviationId = "CUR-W2";

    /// <summary>The configured rate deviates from the live reference feed.</summary>
    public const string LiveDeviationId = "CUR-W3";

    /// <summary>Fewest explicit-currency row pairs before CUR-W2 speaks about a currency.</summary>
    public const int MinSamplePairs = 3;

    // ---- Layer 1: the Price Explainer's conversion row -------------------------------------

    /// <summary>
    /// Explains the conversion DW applies for <paramref name="contextCurrencyCode"/>, or null
    /// when no conversion happens (the context currency is the default, or is unknown).
    /// </summary>
    public static ConversionExplanation? ExplainConversion(
        IReadOnlyList<CurrencySpec> currencies, string contextCurrencyCode)
    {
        var defaultCurrency = currencies.FirstOrDefault(c => c.IsDefault);
        var context = currencies.FirstOrDefault(c =>
            string.Equals(c.Code, contextCurrencyCode, StringComparison.OrdinalIgnoreCase));

        if (defaultCurrency is null || context is null || context.Code.Equals(defaultCurrency.Code, StringComparison.OrdinalIgnoreCase))
            return null;

        var findings = new List<Finding>();
        AddStructuralFindings(findings, defaultCurrency, context);

        var factor = context.Rate > 0 ? defaultCurrency.Rate / context.Rate : double.NaN;
        var implied = double.IsNaN(factor)
            ? $"1 {defaultCurrency.Code} → undefined (rate {N(context.Rate)})"
            : $"1 {defaultCurrency.Code} ≈ {N(factor)} {context.Code}";
        var formula = double.IsNaN(factor)
            ? $"final = base × {defaultCurrency.Code}.Rate {N(defaultCurrency.Rate)} ÷ {context.Code}.Rate {N(context.Rate)} — division by a non-positive rate (DW PriceCalculator.Exchange)"
            : $"final = base × {defaultCurrency.Code}.Rate {N(defaultCurrency.Rate)} ÷ {context.Code}.Rate {N(context.Rate)} = ×{N(factor)} (DW PriceCalculator.Exchange)";

        return new ConversionExplanation(defaultCurrency.Code, context.Code, factor, implied, formula, findings);
    }

    // ---- Layer 2: the Health screen's solution-wide rules -----------------------------------

    /// <summary>
    /// Every structural rate problem, plus the internal-consistency check: products priced
    /// explicitly in two currencies imply an exchange rate; a configured rate that contradicts
    /// the median implication by more than <paramref name="deviationPercent"/> is reported.
    /// </summary>
    public static IEnumerable<Finding> Evaluate(
        IReadOnlyList<CurrencySpec> currencies,
        IReadOnlyList<CurrencyPriceSample> samples,
        int deviationPercent)
    {
        if (currencies.Count == 0)
            yield break;

        var defaultCurrency = currencies.FirstOrDefault(c => c.IsDefault);
        if (defaultCurrency is null)
        {
            yield return new Finding(NoDefaultId, FindingSeverity.Critical, Operations.Rules.OperationsEntities.Currency,
                "(default)", "Default currency",
                "No default currency is defined",
                "Every price calculation and currency conversion starts from the default currency — define one under Settings → Commerce.");
            yield break;
        }

        if (!Near(defaultCurrency.Rate, 100))
            yield return DefaultRateFinding(defaultCurrency);

        foreach (var c in currencies.Where(c => c.Rate <= 0))
            yield return new Finding(NonPositiveRateId, FindingSeverity.Critical, Operations.Rules.OperationsEntities.Currency,
                c.Code, $"{c.Code} — {c.Name}",
                $"Exchange rate is {N(c.Rate)}",
                $"DW's conversion divides by this rate (PriceCalculator.Exchange), so {c.Code} prices are undefined or infinite. Set the rate to the value of 100 {c.Code} in {defaultCurrency.Code}.");

        foreach (var c in currencies.Where(c => !c.IsDefault && c.Rate > 0 && Near(c.Rate, 100)))
            yield return new Finding(UntouchedRateId, FindingSeverity.Warning, Operations.Rules.OperationsEntities.Currency,
                c.Code, $"{c.Code} — {c.Name}",
                "Exchange rate is exactly 100 — likely never maintained",
                $"Rate 100 is what a currency starts with and means 1 {c.Code} = 1 {defaultCurrency.Code}. If that parity is not intended, set the rate to the value of 100 {c.Code} in {defaultCurrency.Code}.");

        foreach (var finding in MatrixDeviationFindings(currencies, defaultCurrency, samples, deviationPercent))
            yield return finding;
    }

    // ---- Layer 3: the live reference comparison ---------------------------------------------

    /// <summary>
    /// Compares configured rates against a EUR-based reference feed ("1 EUR = X units"), e.g.
    /// the ECB daily reference rates. Currencies absent from the feed are skipped; without the
    /// default currency in the feed nothing can be derived at all.
    /// </summary>
    public static IEnumerable<Finding> CompareLive(
        IReadOnlyList<CurrencySpec> currencies,
        IReadOnlyDictionary<string, double> eurRates,
        int deviationPercent)
    {
        var defaultCurrency = currencies.FirstOrDefault(c => c.IsDefault);
        if (defaultCurrency is null || !TryRate(eurRates, defaultCurrency.Code, out var eurToDefault))
            yield break;

        foreach (var c in currencies.Where(c => !c.IsDefault && c.Rate > 0))
        {
            if (!TryRate(eurRates, c.Code, out var eurToCurrency) || eurToCurrency <= 0)
                continue;

            // DW semantics: Rate = value of 100 units of the currency in the default currency.
            var expected = 100d * eurToDefault / eurToCurrency;
            var deviation = Deviation(c.Rate, expected);
            if (deviation * 100 <= deviationPercent)
                continue;

            yield return new Finding(LiveDeviationId, FindingSeverity.Warning, Operations.Rules.OperationsEntities.Currency,
                c.Code, $"{c.Code} — {c.Name}",
                $"Rate {N(c.Rate)} is {N(deviation * 100)}% off the live reference",
                $"The reference feed implies 100 {c.Code} ≈ {N(expected)} {defaultCurrency.Code} (expected Rate ≈ {N(expected)}), but the configured rate is {N(c.Rate)}.");
        }
    }

    // ---- Internals ----------------------------------------------------------------------------

    private static void AddStructuralFindings(List<Finding> findings, CurrencySpec defaultCurrency, CurrencySpec context)
    {
        if (!Near(defaultCurrency.Rate, 100))
            findings.Add(DefaultRateFinding(defaultCurrency));

        if (context.Rate <= 0)
            findings.Add(new Finding(NonPositiveRateId, FindingSeverity.Critical, Operations.Rules.OperationsEntities.Currency,
                context.Code, $"{context.Code} — {context.Name}",
                $"Exchange rate is {N(context.Rate)}",
                $"{context.Code} has rate {N(context.Rate)} and DW divides by it — the converted price is undefined. Set the rate to the value of 100 {context.Code} in {defaultCurrency.Code}."));
        else if (!defaultCurrency.Code.Equals(context.Code, StringComparison.OrdinalIgnoreCase) && Near(context.Rate, 100))
            findings.Add(new Finding(UntouchedRateId, FindingSeverity.Warning, Operations.Rules.OperationsEntities.Currency,
                context.Code, $"{context.Code} — {context.Name}",
                "Exchange rate is exactly 100 — likely never maintained",
                $"Rate 100 means 1 {context.Code} = 1 {defaultCurrency.Code}. If that parity is not intended, this price is converted with a placeholder rate."));
    }

    private static Finding DefaultRateFinding(CurrencySpec defaultCurrency)
    {
        var scale = defaultCurrency.Rate / 100d;
        return new Finding(DefaultRateId, FindingSeverity.Critical, Operations.Rules.OperationsEntities.Currency,
            defaultCurrency.Code, $"{defaultCurrency.Code} — {defaultCurrency.Name} (default)",
            $"Default currency rate is {N(defaultCurrency.Rate)}, not 100",
            $"DW converts with converted = price × default.Rate ÷ currency.Rate (PriceCalculator.Exchange) and expects the default currency's rate to be exactly 100. " +
            $"With rate {N(defaultCurrency.Rate)}, every price converted out of {defaultCurrency.Code} is scaled by ×{N(scale)}" +
            $"{(scale < 1 ? $" — roughly {N(1 / scale)}× too low" : scale > 1 ? $" — roughly {N(scale)}× too high" : string.Empty)}. " +
            $"Set {defaultCurrency.Code}'s rate to 100.");
    }

    private static IEnumerable<Finding> MatrixDeviationFindings(
        IReadOnlyList<CurrencySpec> currencies,
        CurrencySpec defaultCurrency,
        IReadOnlyList<CurrencyPriceSample> samples,
        int deviationPercent)
    {
        if (samples.Count == 0 || deviationPercent <= 0)
            yield break;

        // Pair rows for the same product/variant/quantity/VAT-mode: one in the default
        // currency, one in another explicit currency. Each pair implies what 100 units of the
        // other currency are worth in the default currency.
        var implied = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in samples
                     .Where(s => !string.IsNullOrWhiteSpace(s.CurrencyCode) && s.Amount > 0)
                     .GroupBy(s => (s.ProductId, s.VariantId, s.Quantity, s.IsWithVat)))
        {
            var defaults = group.Where(s => s.CurrencyCode.Equals(defaultCurrency.Code, StringComparison.OrdinalIgnoreCase)).ToList();
            if (defaults.Count != 1)
                continue; // Ambiguous or missing anchor — no exchange information.

            foreach (var other in group.Where(s => !s.CurrencyCode.Equals(defaultCurrency.Code, StringComparison.OrdinalIgnoreCase)))
            {
                if (!implied.TryGetValue(other.CurrencyCode, out var list))
                    implied[other.CurrencyCode] = list = [];
                list.Add(100d * defaults[0].Amount / other.Amount);
            }
        }

        foreach (var (code, values) in implied.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (values.Count < MinSamplePairs)
                continue;

            var currency = currencies.FirstOrDefault(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (currency is null || currency.Rate <= 0)
                continue; // Unknown code or already reported as CUR-C2.

            var median = Median(values);
            var deviation = Deviation(currency.Rate, median);
            if (deviation * 100 <= deviationPercent)
                continue;

            yield return new Finding(MatrixDeviationId, FindingSeverity.Warning, Operations.Rules.OperationsEntities.Currency,
                currency.Code, $"{currency.Code} — {currency.Name}",
                $"Configured rate {N(currency.Rate)} contradicts the price matrix ({N(deviation * 100)}% off)",
                $"{values.Count} product(s) priced explicitly in both {currency.Code} and {defaultCurrency.Code} imply 100 {currency.Code} ≈ {N(median)} {defaultCurrency.Code}, " +
                $"but the configured rate is {N(currency.Rate)}. One of the two is wrong — converted prices and explicit prices will disagree.");
        }
    }

    private static bool TryRate(IReadOnlyDictionary<string, double> rates, string code, out double rate)
    {
        foreach (var (key, value) in rates)
        {
            if (key.Equals(code, StringComparison.OrdinalIgnoreCase))
            {
                rate = value;
                return rate > 0;
            }
        }

        rate = 0;
        return false;
    }

    private static double Deviation(double actual, double expected) =>
        expected <= 0 ? 0 : Math.Abs(actual - expected) / expected;

    private static double Median(List<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2d;
    }

    private static bool Near(double value, double target) => Math.Abs(value - target) < 0.0001;

    /// <summary>Invariant, trimmed number — rates and factors, not money.</summary>
    private static string N(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);
}
