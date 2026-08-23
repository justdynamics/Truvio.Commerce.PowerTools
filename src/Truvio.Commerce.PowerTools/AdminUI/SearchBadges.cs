using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Lists;

namespace Truvio.Commerce.PowerTools.AdminUI;

/// <summary>
/// Badges for the Search section, in the same colour language as the rest of the suite:
/// green = healthy, red = broken, amber = needs attention.
/// </summary>
internal static class SearchBadges
{
    // NOTE: a Badge with an Icon renders icon-only (the Value text is dropped), so every
    // badge here is text-only.

    public static Cell Health(string healthKind, string text) =>
        Cell.MakeCell(new Badge
        {
            Value = text,
            BadgeType = healthKind switch
            {
                "Ok" => BadgeType.Success,
                "Stale" => BadgeType.Warning,
                "NeverBuilt" => BadgeType.Danger,
                "Failed" => BadgeType.Danger,
                _ => BadgeType.Muted
            }
        });

    public static Cell FieldStatus(string status) =>
        Cell.MakeCell(new Badge
        {
            Value = status,
            BadgeType = status switch
            {
                "Dangling" => BadgeType.Danger,
                "Unused" => BadgeType.Warning,
                "Used" => BadgeType.Success,
                _ => BadgeType.Muted
            }
        });
}
