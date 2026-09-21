namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

/// <summary>
/// Pure, DW-free snapshot records describing what lives in the repositories: indexes with
/// their schema, instances and builders; queries with their parameters, expression tree and
/// sort order; facet groups. Everything the Index &amp; Query Inspector reasons about is
/// expressed over these records so the rules stay unit-testable.
/// </summary>
public sealed record IndexFieldSpec(
    string SystemName,
    string Name,
    string TypeName,
    string Analyzer,
    string Boost,
    bool Stored,
    bool Indexed,
    bool Analyzed,
    string Group,
    string Source,
    string Kind)
{
    /// <summary>"System.String[]" -&gt; "String[]" for display.</summary>
    public string ShortTypeName => ShortenType(TypeName);

    internal static string ShortenType(string? typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return string.Empty;

        var name = typeName.Split(',')[0].Trim();
        var array = name.EndsWith("[]", StringComparison.Ordinal);
        if (array)
            name = name[..^2];

        var dot = name.LastIndexOf('.');
        if (dot >= 0 && dot < name.Length - 1)
            name = name[(dot + 1)..];

        return array ? name + "[]" : name;
    }
}
