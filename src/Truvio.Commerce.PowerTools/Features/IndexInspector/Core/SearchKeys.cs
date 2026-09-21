namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

/// <summary>Repository item keys are compared case-insensitively, like the file system.</summary>
public static class SearchKeys
{
    public static string For(string? repository, string? item) =>
        string.IsNullOrEmpty(repository) || string.IsNullOrEmpty(item)
            ? string.Empty
            : $"{repository}/{item}";

    public static bool Same(string? a, string? b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
