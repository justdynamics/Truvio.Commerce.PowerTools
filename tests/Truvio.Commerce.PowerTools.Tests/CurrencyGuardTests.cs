using Truvio.Commerce.PowerTools.Core.Commerce;
using Truvio.Commerce.PowerTools.Core.Diagnostics;
using Xunit;

namespace Truvio.Commerce.PowerTools.Tests;

public class CurrencyGuardTests
{
    private static CurrencySpec Usd(double rate = 100, bool isDefault = true) => new("USD", "US Dollar", rate, isDefault);
    private static CurrencySpec Sek(double rate) => new("SEK", "Swedish Krona", rate, false);
    private static CurrencySpec Eur(double rate) => new("EUR", "Euro", rate, false);

    // ---- ExplainConversion (layer 1) --------------------------------------------------------

    [Fact]
    public void ContextIsDefault_NoConversionRow()
    {
        Assert.Null(CurrencyGuard.ExplainConversion([Usd(), Sek(9.5)], "USD"));
    }

    [Fact]
    public void UnknownContextCurrency_NoConversionRow()
    {
        Assert.Null(CurrencyGuard.ExplainConversion([Usd(), Sek(9.5)], "NOK"));
    }

    [Fact]
    public void HealthyRates_FactorIsHundredOverRate_AndClean()
    {
        var conv = CurrencyGuard.ExplainConversion([Usd(), Sek(9.5)], "SEK");

        Assert.NotNull(conv);
        Assert.Equal(100d / 9.5, conv.Factor, 6);
        Assert.False(conv.Broken);
        Assert.Empty(conv.Findings);
        Assert.Contains("SEK", conv.ImpliedRateText);
    }

    [Fact]
    public void MarineScenario_DefaultRateOne_IsBrokenWithHundredfoldExplanation()
    {
        // The observed bug: USD default seeded with Rate=1 → $16.75 became kr1.86.
        var conv = CurrencyGuard.ExplainConversion([Usd(rate: 1), Sek(9)], "SEK");

        Assert.NotNull(conv);
        Assert.Equal(1d / 9, conv.Factor, 6);
        Assert.True(conv.Broken);
        var finding = Assert.Single(conv.Findings);
        Assert.Equal(CurrencyGuard.DefaultRateId, finding.RuleId);
        Assert.Equal(FindingSeverity.Critical, finding.Severity);
        Assert.Contains("100× too low", finding.Detail);
    }

    [Fact]
    public void ZeroContextRate_IsBroken_NotAnException()
    {
        var conv = CurrencyGuard.ExplainConversion([Usd(), Sek(0)], "SEK");

        Assert.NotNull(conv);
        Assert.True(double.IsNaN(conv.Factor));
        Assert.Contains(conv.Findings, f => f.RuleId == CurrencyGuard.NonPositiveRateId && f.Severity == FindingSeverity.Critical);
    }

    [Fact]
    public void UntouchedHundred_OnContextCurrency_Warns()
    {
        var conv = CurrencyGuard.ExplainConversion([Usd(), Eur(100)], "EUR");

        Assert.NotNull(conv);
        Assert.False(conv.Broken);
        var finding = Assert.Single(conv.Findings);
        Assert.Equal(CurrencyGuard.UntouchedRateId, finding.RuleId);
        Assert.Equal(FindingSeverity.Warning, finding.Severity);
    }

    // ---- Evaluate (layer 2) ------------------------------------------------------------------

    [Fact]
    public void NoCurrencies_NoFindings()
    {
        Assert.Empty(CurrencyGuard.Evaluate([], [], 25));
    }

    [Fact]
    public void NoDefaultCurrency_IsCritical()
    {
        var findings = CurrencyGuard.Evaluate([Sek(9.5)], [], 25).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal(CurrencyGuard.NoDefaultId, finding.RuleId);
        Assert.Equal(FindingSeverity.Critical, finding.Severity);
    }

    [Fact]
    public void MarineTable_ReportsDefaultRate_AndUntouchedEur()
    {
        var findings = CurrencyGuard.Evaluate([Usd(rate: 1), Sek(9), Eur(100)], [], 25).ToList();

        Assert.Contains(findings, f => f.RuleId == CurrencyGuard.DefaultRateId && f.EntityKey == "USD");
        Assert.Contains(findings, f => f.RuleId == CurrencyGuard.UntouchedRateId && f.EntityKey == "EUR");
        Assert.DoesNotContain(findings, f => f.EntityKey == "SEK");
    }

    [Fact]
    public void HealthyTable_IsSilent()
    {
        Assert.Empty(CurrencyGuard.Evaluate([Usd(), Sek(9.5), Eur(92)], [], 25));
    }

    [Fact]
    public void NegativeRate_IsCritical()
    {
        var findings = CurrencyGuard.Evaluate([Usd(), Sek(-5)], [], 25).ToList();

        Assert.Contains(findings, f => f.RuleId == CurrencyGuard.NonPositiveRateId && f.EntityKey == "SEK");
    }

    // ---- Matrix consistency (CUR-W2) --------------------------------------------------------

    private static CurrencyPriceSample Row(string product, string currency, double amount, double qty = 1) =>
        new(product, string.Empty, currency, qty, amount, IsWithVat: false);

    private static IReadOnlyList<CurrencyPriceSample> PairedRows(double usd, double sek, int products) =>
        Enumerable.Range(1, products)
            .SelectMany(i => new[] { Row($"P{i}", "USD", usd), Row($"P{i}", "SEK", sek) })
            .ToList();

    [Fact]
    public void MatrixContradiction_Warns_WithImpliedRate()
    {
        // 16.75 USD priced as 160 SEK implies 100 SEK ≈ 10.47 USD; a configured rate of 90 is
        // ~760% off — someone typed the rate against the wrong base.
        var findings = CurrencyGuard.Evaluate([Usd(), Sek(90)], PairedRows(16.75, 160, products: 3), 25).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal(CurrencyGuard.MatrixDeviationId, finding.RuleId);
        Assert.Equal("SEK", finding.EntityKey);
        Assert.Contains("10.4", finding.Detail);
    }

    [Fact]
    public void MatrixAgreement_IsSilent()
    {
        // Implied 10.47 vs configured 9.5 is within a 25% tolerance.
        Assert.Empty(CurrencyGuard.Evaluate([Usd(), Sek(9.5)], PairedRows(16.75, 160, products: 3), 25));
    }

    [Fact]
    public void FewerPairsThanMinimum_IsSilent()
    {
        Assert.Empty(CurrencyGuard.Evaluate([Usd(), Sek(90)], PairedRows(16.75, 160, products: CurrencyGuard.MinSamplePairs - 1), 25));
    }

    [Fact]
    public void VatModeMismatch_RowsAreNotPaired()
    {
        var samples = new List<CurrencyPriceSample>();
        for (var i = 1; i <= 3; i++)
        {
            samples.Add(new($"P{i}", "", "USD", 1, 16.75, IsWithVat: false));
            samples.Add(new($"P{i}", "", "SEK", 1, 160, IsWithVat: true));
        }

        Assert.Empty(CurrencyGuard.Evaluate([Usd(), Sek(90)], samples, 25));
    }

    [Fact]
    public void AmbiguousDefaultAnchor_IsSkipped()
    {
        // Two USD rows for the same product/qty — no way to know which anchors the pair.
        var samples = Enumerable.Range(1, 3).SelectMany(i => new[]
        {
            Row($"P{i}", "USD", 16.75), Row($"P{i}", "USD", 20), Row($"P{i}", "SEK", 160)
        }).ToList();

        Assert.Empty(CurrencyGuard.Evaluate([Usd(), Sek(90)], samples, 25));
    }

    // ---- CompareLive (layer 3) ---------------------------------------------------------------

    private static readonly IReadOnlyDictionary<string, double> EurFeed =
        new Dictionary<string, double> { ["EUR"] = 1, ["USD"] = 1.08, ["SEK"] = 11.3 };

    [Fact]
    public void LiveAgreement_IsSilent()
    {
        // Expected SEK rate = 100 × 1.08 / 11.3 ≈ 9.56; configured 9.5 is fine.
        Assert.Empty(CurrencyGuard.CompareLive([Usd(), Sek(9.5)], EurFeed, 25));
    }

    [Fact]
    public void LiveContradiction_Warns()
    {
        var findings = CurrencyGuard.CompareLive([Usd(), Sek(90)], EurFeed, 25).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal(CurrencyGuard.LiveDeviationId, finding.RuleId);
        Assert.Equal("SEK", finding.EntityKey);
    }

    [Fact]
    public void DefaultCurrencyMissingFromFeed_NothingDerivable()
    {
        var feed = new Dictionary<string, double> { ["EUR"] = 1, ["SEK"] = 11.3 };

        Assert.Empty(CurrencyGuard.CompareLive([Usd(), Sek(90)], feed, 25));
    }

    [Fact]
    public void CurrencyMissingFromFeed_IsSkipped()
    {
        var findings = CurrencyGuard.CompareLive([Usd(), Sek(90), new("XXX", "Testium", 5, false)], EurFeed, 25).ToList();

        Assert.All(findings, f => Assert.Equal("SEK", f.EntityKey));
    }

    // ---- ECB feed parsing ---------------------------------------------------------------------

    [Fact]
    public void EcbXml_Parses_AndIncludesEur()
    {
        const string xml = """
            <gesmes:Envelope xmlns:gesmes="http://www.gesmes.org/xml/2002-08-01" xmlns="http://www.ecb.int/vocabulary/2002-08-01/eurofxref">
              <Cube><Cube time="2026-08-24">
                <Cube currency="USD" rate="1.08"/>
                <Cube currency="SEK" rate="11.30"/>
              </Cube></Cube>
            </gesmes:Envelope>
            """;

        var rates = EcbRateSource.Parse(xml);

        Assert.NotNull(rates);
        Assert.Equal(1d, rates["EUR"]);
        Assert.Equal(1.08, rates["USD"]);
        Assert.Equal(11.30, rates["SEK"]);
    }

    [Fact]
    public void GarbageXml_ParsesToNull()
    {
        Assert.Null(EcbRateSource.Parse("not xml"));
        Assert.Null(EcbRateSource.Parse("<empty/>"));
    }
}
