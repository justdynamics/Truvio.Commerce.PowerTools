namespace Truvio.Commerce.PowerTools.Features.QueryTester.Core;

/// <summary>A concrete edit the user can make, ranked by <see cref="Kind"/>.</summary>
public sealed record Suggestion(string Kind, string Title, string Detail)
{
    public static Suggestion Fix(string title, string detail) => new("fix", title, detail);

    public static Suggestion Warn(string title, string detail) => new("warn", title, detail);

    public static Suggestion Info(string title, string detail) => new("info", title, detail);

    /// <summary>Sort weight — problems that break the result come first.</summary>
    public int Rank => Kind switch { "fix" => 0, "warn" => 1, _ => 2 };
}
