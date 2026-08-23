using System.Text;

namespace Truvio.Commerce.PowerTools.Core.Search.Testing;

/// <summary>
/// The URL syntax the Query tester uses to carry parameter values:
/// <c>name=value;name2=value2</c>.
/// <para>
/// An overview screen has no form input, so the values have to survive in the screen URL.
/// Names are taken verbatim from the query's <c>&lt;Parameter Name="…"&gt;</c> declarations
/// (they may contain spaces — "Bike type" is a real one), the first <c>=</c> separates name
/// from value, and <c>;</c> separates assignments. A value may therefore contain <c>=</c>
/// but not <c>;</c>; a value that needs a semicolon cannot be expressed here, which is
/// stated on the screen.
/// </para>
/// <para>
/// A name with an EMPTY value is kept in the text (so the user can see they cleared it) but
/// is never handed to the index provider: <c>LuceneQueryProvider.HandleParameters</c> leaves
/// an empty supplied string in the dictionary, and
/// <c>Helpers.GetValueFromExpression</c> would then return <c>""</c> instead of null — the
/// clause would compare against an empty string rather than disappearing. Omitting the key
/// reproduces what the frontend actually does (<c>QueryHelper.ParseQueryParameters</c> skips
/// values that <c>ValueConverter.ConvertString</c> turns into null).
/// </para>
/// </summary>
public static class ParameterValues
{
    /// <summary>
    /// Names starting with '#' are the tester's own settings rather than query parameters, so
    /// they ride along in the same string but are never handed to the index provider.
    /// </summary>
    public const char ReservedPrefix = '#';

    /// <summary>The document key the "Why not X?" section explains ("#expect=PROD27").</summary>
    public const string ExpectKeyName = "#expect";

    /// <summary>The value of a reserved setting, or an empty string.</summary>
    public static string Reserved(string? text, string name) =>
        Parse(text).TryGetValue(name, out var value) ? value : string.Empty;

    public static bool IsReserved(string? name) =>
        !string.IsNullOrEmpty(name) && name[0] == ReservedPrefix;

    /// <summary>Parses <c>name=value;name2=value2</c>. Later assignments win.</summary>
    public static IReadOnlyDictionary<string, string> Parse(string? text)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(text))
            return result;

        foreach (var part in text.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
                continue;

            var name = part[..separator].Trim();
            if (name.Length == 0)
                continue;

            result[name] = part[(separator + 1)..].Trim();
        }

        return result;
    }

    /// <summary>Only the assignments that actually reach the provider (non-empty values).</summary>
    public static IReadOnlyDictionary<string, string> Effective(string? text)
    {
        var all = Parse(text);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in all)
        {
            if (!string.IsNullOrEmpty(pair.Value) && !IsReserved(pair.Key))
                result[pair.Key] = pair.Value;
        }

        return result;
    }

    public static string Format(IEnumerable<KeyValuePair<string, string>> values)
    {
        var sb = new StringBuilder();
        foreach (var pair in values)
        {
            if (string.IsNullOrEmpty(pair.Key))
                continue;
            if (sb.Length > 0)
                sb.Append(';');
            sb.Append(pair.Key).Append('=').Append(pair.Value);
        }

        return sb.ToString();
    }

    /// <summary>Adds or replaces one assignment; a null value removes it entirely.</summary>
    public static string Set(string? text, string name, string? value)
    {
        var ordered = Ordered(text);
        var at = ordered.FindIndex(p => string.Equals(p.Key, name, StringComparison.OrdinalIgnoreCase));

        if (value is null)
        {
            if (at >= 0)
                ordered.RemoveAt(at);
        }
        else if (at >= 0)
        {
            // Replace in place, so the URL keeps a stable order as the user edits values.
            ordered[at] = new KeyValuePair<string, string>(ordered[at].Key, value);
        }
        else
        {
            ordered.Add(new KeyValuePair<string, string>(name, value));
        }

        return Format(ordered);
    }

    /// <summary>
    /// Merges a typed assignment list into an existing one — how the toolbar search box on the
    /// parameter screen feeds values in. Anything without an <c>=</c> is ignored.
    /// </summary>
    public static string Merge(string? text, string? assignments)
    {
        if (string.IsNullOrWhiteSpace(assignments) || !assignments.Contains('='))
            return text ?? string.Empty;

        var result = text ?? string.Empty;
        foreach (var pair in Ordered(assignments))
            result = Set(result, pair.Key, pair.Value);

        return result;
    }

    /// <summary>Every declared parameter that has a default, as an assignment list.</summary>
    public static string Defaults(QuerySpec query) =>
        Format(query.Parameters
            .Where(p => p.HasDefault)
            .Select(p => new KeyValuePair<string, string>(p.Name, p.DefaultValue)));

    private static List<KeyValuePair<string, string>> Ordered(string? text)
    {
        var result = new List<KeyValuePair<string, string>>();
        if (string.IsNullOrWhiteSpace(text))
            return result;

        foreach (var part in text.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
                continue;

            var name = part[..separator].Trim();
            if (name.Length == 0)
                continue;

            result.RemoveAll(p => string.Equals(p.Key, name, StringComparison.OrdinalIgnoreCase));
            result.Add(new KeyValuePair<string, string>(name, part[(separator + 1)..].Trim()));
        }

        return result;
    }
}
