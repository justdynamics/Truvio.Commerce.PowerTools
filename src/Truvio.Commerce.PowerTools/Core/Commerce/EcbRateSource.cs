using System.Globalization;
using System.Xml.Linq;

namespace Truvio.Commerce.PowerTools.Core.Commerce;

/// <summary>
/// The optional live reference for CUR-W3: the ECB daily reference rates, a public EUR-based
/// XML feed. Off by default — the suite makes no outbound call unless the admin enables the
/// live rate check in PowerTools settings. One fetch per health run, short timeout, and any
/// failure degrades to "no live comparison" rather than a finding.
/// </summary>
public static class EcbRateSource
{
    public const string DefaultUrl = "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };

    /// <summary>"1 EUR = X units" per currency code (EUR itself included), or null when the feed is unreachable.</summary>
    public static IReadOnlyDictionary<string, double>? Fetch(string? url = null)
    {
        try
        {
            var xml = Http.GetStringAsync(string.IsNullOrWhiteSpace(url) ? DefaultUrl : url)
                .GetAwaiter().GetResult();
            return Parse(xml);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Parses the eurofxref XML: <c>&lt;Cube currency="USD" rate="1.08"/&gt;</c> per currency.</summary>
    public static IReadOnlyDictionary<string, double>? Parse(string xml)
    {
        try
        {
            var rates = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["EUR"] = 1d };
            foreach (var cube in XDocument.Parse(xml).Descendants())
            {
                var code = cube.Attribute("currency")?.Value;
                var rate = cube.Attribute("rate")?.Value;
                if (!string.IsNullOrEmpty(code) && double.TryParse(rate, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > 0)
                    rates[code] = value;
            }

            return rates.Count > 1 ? rates : null;
        }
        catch
        {
            return null;
        }
    }
}
