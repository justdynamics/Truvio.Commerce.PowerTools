using System.Globalization;

namespace Truvio.Commerce.PowerTools.Core.Operations;

/// <summary>
/// Pure display helpers shared by the Operations screens. Kept in Core (not in the AdminUI
/// layer) so they are unit-tested — every one of them feeds a narrow list column where an
/// off-by-one reads as a bug to the ops person looking at it.
/// </summary>
public static class OpsFormat
{
    /// <summary>
    /// "Ns.Sub.MyAddIn, MyAssembly" → "MyAddIn". Handles bare type names, assembly-qualified
    /// names and empty input.
    /// </summary>
    public static string ShortTypeName(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return string.Empty;

        var value = typeName.Trim();

        var comma = value.IndexOf(',');
        if (comma >= 0)
            value = value[..comma].Trim();

        var dot = value.LastIndexOf('.');
        if (dot >= 0 && dot < value.Length - 1)
            value = value[(dot + 1)..];

        return value;
    }

    /// <summary>Binary size with one decimal, e.g. 1536 → "1.5 KB".</summary>
    public static string Bytes(long bytes)
    {
        if (bytes < 0)
            return "-";
        if (bytes < 1024)
            return $"{bytes} B";

        string[] units = ["KB", "MB", "GB", "TB", "PB"];
        double value = bytes;
        var unit = -1;
        do
        {
            value /= 1024;
            unit++;
        }
        while (value >= 1024 && unit < units.Length - 1);

        return string.Create(CultureInfo.InvariantCulture, $"{value:0.#} {units[unit]}");
    }

    /// <summary>
    /// Compact relative age: "just now", "5 min ago", "3 h ago", "2 d ago", "in 4 h", "never".
    /// </summary>
    public static string Relative(DateTime? moment, DateTime now)
    {
        if (moment is not { } value)
            return "never";

        var delta = now - value;
        var future = delta < TimeSpan.Zero;
        var abs = future ? -delta : delta;

        var text = abs.TotalSeconds switch
        {
            < 60 => "just now",
            < 3600 => $"{(int)abs.TotalMinutes} min",
            < 86400 => $"{(int)abs.TotalHours} h",
            < 86400 * 60 => $"{(int)abs.TotalDays} d",
            _ => $"{(int)(abs.TotalDays / 30)} mo"
        };

        if (text == "just now")
            return text;

        return future ? $"in {text}" : $"{text} ago";
    }

    /// <summary>Absolute timestamp in a sortable, unambiguous form; "-" when null.</summary>
    public static string Absolute(DateTime? moment) =>
        moment is { } value ? value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : "-";

    /// <summary>Elapsed time, e.g. "0.4 s", "12 s", "3 m 05 s", "1 h 12 m"; "-" when null.</summary>
    public static string Duration(TimeSpan? duration)
    {
        if (duration is not { } d || d < TimeSpan.Zero)
            return "-";

        if (d.TotalSeconds < 1)
            return string.Create(CultureInfo.InvariantCulture, $"{d.TotalSeconds:0.0} s");
        if (d.TotalSeconds < 60)
            return string.Create(CultureInfo.InvariantCulture, $"{(int)d.TotalSeconds} s");
        if (d.TotalMinutes < 60)
            return string.Create(CultureInfo.InvariantCulture, $"{(int)d.TotalMinutes} m {d.Seconds:00} s");

        return string.Create(CultureInfo.InvariantCulture, $"{(int)d.TotalHours} h {d.Minutes:00} m");
    }

    /// <summary>Repeat interval in minutes as a short phrase, e.g. 1440 → "every 1 d".</summary>
    public static string Interval(int minutes) => minutes switch
    {
        <= 0 => "once",
        < 60 => $"every {minutes} min",
        < 1440 when minutes % 60 == 0 => $"every {minutes / 60} h",
        < 1440 => $"every {minutes / 60} h {minutes % 60} min",
        _ when minutes % 1440 == 0 => $"every {minutes / 1440} d",
        _ => $"every {minutes / 1440} d {(minutes % 1440) / 60} h"
    };
}
