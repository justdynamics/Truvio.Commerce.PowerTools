using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Lists;
using Truvio.Commerce.PowerTools.Core.Permissions;

namespace Truvio.Commerce.PowerTools.AdminUI;

/// <summary>
/// One consistent colour language across all Security Viewer screens:
/// green = the account sees it, red = denied, amber = caution/bypass,
/// outline shades = where the outcome came from.
/// </summary>
internal static class Badges
{
    // NOTE: a Badge with an Icon renders icon-only (the Value text is dropped), so every
    // badge that must stay readable is text-only.

    // Outline badge styles exist from DW 10.23; older hosts get the solid equivalents.
#if DW_HAS_OUTLINE_BADGES
    private const BadgeType OutlineMutedOrFallback = BadgeType.OutlineMuted;
    private const BadgeType OutlinePrimaryOrFallback = BadgeType.OutlinePrimary;
    private const BadgeType OutlineInfoOrFallback = BadgeType.OutlineInfo;
#else
    private const BadgeType OutlineMutedOrFallback = BadgeType.Muted;
    private const BadgeType OutlinePrimaryOrFallback = BadgeType.Primary;
    private const BadgeType OutlineInfoOrFallback = BadgeType.Info;
#endif

    public static Cell Visible(bool visible, string text) =>
        Cell.MakeCell(new Badge
        {
            Value = text,
            BadgeType = visible ? BadgeType.Success : BadgeType.Danger
        });

    public static Cell Level(int levelValue, string text) =>
        Cell.MakeCell(new Badge
        {
            Value = text,
            BadgeType = levelValue switch
            {
                Levels.NotSet => OutlineMutedOrFallback,
                Levels.None => BadgeType.Danger,
                Levels.Read => BadgeType.Success,
                Levels.Edit => BadgeType.Info,
                Levels.Create => BadgeType.Info,
                Levels.Delete => BadgeType.Warning,
                Levels.All => BadgeType.Primary,
                _ => BadgeType.Muted
            }
        });

    /// <summary>
    /// Explicit and inherited outcomes get a visible badge; role defaults and page
    /// fallbacks stay plain text (null = default cell) — they are the unremarkable case,
    /// and DW's muted outline badge is illegible anyway.
    /// </summary>
    public static Cell? Origin(string originKind, string text) =>
        originKind switch
        {
            nameof(AccessOrigin.Bypass) =>
                Cell.MakeCell(new Badge { Value = text, BadgeType = BadgeType.Warning }),
            nameof(AccessOrigin.ExplicitHere) =>
                Cell.MakeCell(new Badge { Value = text, BadgeType = OutlinePrimaryOrFallback }),
            nameof(AccessOrigin.InheritedFromPage) =>
                Cell.MakeCell(new Badge { Value = text, BadgeType = OutlineInfoOrFallback }),
            _ => null
        };

    public static Cell WarningBadge(string text) =>
        Cell.MakeCell(new Badge
        {
            Value = text,
            BadgeType = BadgeType.Danger
        });

    public static Cell Severity(string severity) =>
        Cell.MakeCell(new Badge
        {
            Value = severity,
            BadgeType = severity switch
            {
                "Critical" => BadgeType.Danger,
                "Warning" => BadgeType.Warning,
                _ => BadgeType.Info
            }
        });

    public static Cell AccountKind(string kind) =>
        Cell.MakeCell(new Badge
        {
            Value = kind,
            BadgeType = kind switch
            {
                "Role" => BadgeType.Info,
                "Group" => BadgeType.Primary,
                _ => BadgeType.Secondary
            }
        });
}
