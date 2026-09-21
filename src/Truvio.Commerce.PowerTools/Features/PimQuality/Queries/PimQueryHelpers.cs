using System.Globalization;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Models;
using Truvio.Commerce.PowerTools.Features.PimQuality.Core;
using Truvio.Commerce.PowerTools.Features.PimQuality.Dw;
using Truvio.Commerce.PowerTools.Features.Settings.Core;
using Truvio.Commerce.PowerTools.Features.Settings.Dw;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Queries;

/// <summary>Shared helpers for the PIM-section queries.</summary>
internal static class PimQueryHelpers
{
    public static IPimQualitySource Source() => new DwPimSource();

    /// <summary>The scope every screen shares, with the configured cap applied.</summary>
    public static PimScope Scope(string groupId, string languageId, string? search)
    {
        var settings = DwPowerToolsSettings.Current;
        return new PimScope(
            groupId ?? string.Empty,
            languageId ?? string.Empty,
            PowerToolsSettings.Positive(settings.PimProductCap, PimScope.DefaultProductCap),
            search ?? string.Empty);
    }

    /// <summary>win / warn / reject by score, matching the colour language of the other tools.</summary>
    public static string ScoreKind(int score, int threshold) =>
        score >= 100 ? "win"
        : score >= threshold ? "ok"
        : score == 0 ? "reject"
        : "warn";

    public static string Percent(int value) => $"{value}%";

    public static string Count(int value) => value.ToString("N0", CultureInfo.InvariantCulture);

    public static OpsRowModel Row(string item, string verdict, string kind, string value, string why) => new()
    {
        Item = item,
        Verdict = verdict,
        VerdictKind = kind,
        Value = value,
        Why = why
    };

    /// <summary>The severity badge kind used across the finding tables.</summary>
    public static string SeverityKind(FindingSeverity severity) => severity switch
    {
        FindingSeverity.Critical => "reject",
        FindingSeverity.Warning => "warn",
        _ => "info"
    };
}
