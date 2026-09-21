namespace Truvio.Commerce.PowerTools.Shared.Currency;

/// <summary>One currency as configured in DW, reduced to what the conversion math uses.</summary>
public sealed record CurrencySpec(string Code, string Name, double Rate, bool IsDefault);
