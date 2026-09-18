using System.Globalization;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Shared.Currency;

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
