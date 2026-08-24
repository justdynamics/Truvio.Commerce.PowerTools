using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Lists;

namespace Truvio.Commerce.PowerTools.AdminUI;

/// <summary>
/// The colour language of the PIM screens: green = complete, amber = work to do, red = empty
/// or dead configuration.
/// </summary>
internal static class PimBadges
{
    // NOTE: a Badge with an Icon renders icon-only (the Value text is dropped), so every badge
    // here is text-only.

    /// <summary>Completeness score, coloured against the configured threshold.</summary>
    public static Cell Score(int score, string text) =>
        Cell.MakeCell(new Badge
        {
            Value = text,
            BadgeType = score switch
            {
                >= 100 => BadgeType.Success,
                0 => BadgeType.Danger,
                _ => Threshold(score)
            }
        });

    private static BadgeType Threshold(int score)
    {
        var threshold = Core.Settings.PowerToolsSettings.Positive(
            Core.Settings.Dw.DwPowerToolsSettings.Current.PimCompletenessThreshold,
            Core.Pim.PimQualityEngine.DefaultThreshold);

        return score >= threshold ? BadgeType.Success : BadgeType.Warning;
    }

    /// <summary>Dead / assigned state for completion rules and workflows.</summary>
    public static Cell State(string state, string text) =>
        Cell.MakeCell(new Badge
        {
            Value = text,
            BadgeType = state switch
            {
                "Dead" => BadgeType.Warning,
                "Assigned" => BadgeType.Success,
                _ => BadgeType.Info
            }
        });
}
