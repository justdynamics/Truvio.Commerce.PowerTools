using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Lists;

namespace Truvio.Commerce.PowerTools.AdminUI;

/// <summary>
/// Badge colours for the Operations lists. Same language as the rest of the suite: green = it
/// is doing what it should, red = it is not, amber = it might not be.
/// </summary>
internal static class OpsBadges
{
    // A Badge with an Icon renders icon-only (the Value text is dropped), so every badge here
    // is text-only.

    public static Cell TaskState(string state, string text) =>
        Cell.MakeCell(new Badge
        {
            Value = text,
            BadgeType = state switch
            {
                "failed" => BadgeType.Danger,
                "stale" => BadgeType.Warning,
                // Muted renders as an all-but-invisible badge in the DW10 grid; Secondary is the
                // quietest style that still reads.
                "disabled" => BadgeType.Secondary,
                "ok" => BadgeType.Success,
                _ => BadgeType.Info
            }
        });

    public static Cell ActivityResult(string kind, string text) =>
        Cell.MakeCell(new Badge
        {
            Value = string.IsNullOrEmpty(text) ? "Unknown" : text,
            BadgeType = kind switch
            {
                "ok" => BadgeType.Success,
                "warn" => BadgeType.Warning,
                "reject" or "missing" => BadgeType.Danger,
                _ => BadgeType.Secondary
            }
        });

    public static Cell ChangeSource(string text) =>
        Cell.MakeCell(new Badge
        {
            Value = text,
            BadgeType = text switch
            {
                "Admin commands" => BadgeType.Primary,
                "Audit trail" => BadgeType.Info,
                _ => BadgeType.Secondary
            }
        });
}
