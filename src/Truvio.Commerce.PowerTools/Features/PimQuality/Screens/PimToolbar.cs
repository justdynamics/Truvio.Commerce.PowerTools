using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Layout;
using Icon = Dynamicweb.CoreUI.Icons.Icon;
using Truvio.Commerce.PowerTools.Features.PimQuality.Dw;
using Truvio.Commerce.PowerTools.Shared.AdminUI;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Screens;

/// <summary>Shared toolbar wiring so both PIM screens offer identical scope controls.</summary>
internal static class PimToolbar
{
    public static void AddGroupPicker(ScreenLayout layout, string groupId, Func<string, NavigateScreenAction> onPicked)
    {
        var token = Guid.NewGuid().ToString("N");
        ToolbarSwitch.AddPicker(layout, Label(groupId), Icon.Sitemap,
            new Selectors.PimGroupSelectorProvider(), token, onPicked(token));
    }

    public static void AddLanguageSwitch(ScreenLayout layout, string languageId, Func<string, NavigateScreenAction> onPicked)
    {
        var languages = Safe(() => new DwPimSource().GetLanguages());
        if (languages.Count <= 1)
            return;

        var options = languages
            .Select(l => ToolbarSwitch.Option(
                $"{l.Name} ({l.Id})",
                active: string.Equals(l.Id, languageId, StringComparison.OrdinalIgnoreCase) ||
                        (string.IsNullOrEmpty(languageId) && l.Id == languages[0].Id),
                onPicked(l.Id)))
            .ToList();

        var current = languages.FirstOrDefault(l => string.Equals(l.Id, languageId, StringComparison.OrdinalIgnoreCase));
        ToolbarSwitch.Add(layout, string.IsNullOrEmpty(current.Id) ? "Language" : current.Id, Icon.Globe, options);
    }

    private static string Label(string groupId)
    {
        if (string.IsNullOrEmpty(groupId))
            return "Whole catalog";

        var group = Safe(() => new DwPimSource().GetGroups())
            .FirstOrDefault(g => string.Equals(g.Id, groupId, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrEmpty(group.Name) ? groupId : group.Name;
    }

    private static IReadOnlyList<T> Safe<T>(Func<IReadOnlyList<T>> source)
    {
        try
        {
            return source();
        }
        catch
        {
            return [];
        }
    }
}
