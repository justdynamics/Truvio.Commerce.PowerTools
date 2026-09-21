using Truvio.Commerce.PowerTools.Features.IndexInspector.Dw;

namespace Truvio.Commerce.PowerTools.Features.QueryTester.Dw;

/// <summary>One document of a test run, already rendered for display.</summary>
public sealed record RunDocument(int Ordinal, string Key, string Label, IReadOnlyList<DocumentField> Fields)
{
    public string? Value(string field) =>
        Fields.FirstOrDefault(f => string.Equals(f.Name, field, StringComparison.OrdinalIgnoreCase))?.Value;

    public IReadOnlyDictionary<string, string> AsDictionary()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in Fields)
            result[field.Name] = field.Value;
        return result;
    }
}
