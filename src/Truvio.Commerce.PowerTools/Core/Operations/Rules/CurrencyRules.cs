using Truvio.Commerce.PowerTools.Core.Commerce;
using Truvio.Commerce.PowerTools.Core.Diagnostics;

namespace Truvio.Commerce.PowerTools.Core.Operations.Rules;

/// <summary>
/// CUR-C1..CUR-W3: is the currency table capable of converting a price correctly? Delegates to
/// <see cref="CurrencyGuard"/> — structural rate errors, the price-matrix consistency check,
/// and (when the snapshot carries live reference rates) the live deviation check.
/// </summary>
public sealed class CurrencyConfigurationRule(int deviationPercent) : IOperationsRule
{
    public IEnumerable<Finding> Evaluate(OperationsSnapshot snapshot)
    {
        foreach (var finding in CurrencyGuard.Evaluate(snapshot.Currencies, snapshot.PriceSamples, deviationPercent))
            yield return finding;

        if (snapshot.LiveEurRates is null)
            yield break;

        foreach (var finding in CurrencyGuard.CompareLive(snapshot.Currencies, snapshot.LiveEurRates, deviationPercent))
            yield return finding;
    }
}
