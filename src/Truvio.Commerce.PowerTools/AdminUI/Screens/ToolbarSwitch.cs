using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Layout;
using Icon = Dynamicweb.CoreUI.Icons.Icon;

namespace Truvio.Commerce.PowerTools.AdminUI.Screens;

/// <summary>
/// The toolbar treatment for a screen's context switches: a split button next to the Actions
/// menu, labelled with the value in effect, whose menu lists the options with the active one
/// check-marked. Used from <c>ScreenInjector</c> subclasses because the screen bases keep
/// their <c>ScreenLayout</c> private — <c>AddAction</c> is the bar, <c>ContextActionGroups</c>
/// is the Actions dropdown.
/// </summary>
internal static class ToolbarSwitch
{
    /// <summary>A switch: label shows the current value, the menu holds the options.</summary>
    public static void Add(ScreenLayout layout, string label, Icon icon, IEnumerable<ActionNode> options) =>
        layout.AddAction(new Button
        {
            Name = label,
            Icon = icon,
            Type = Button.ButtonType.Secondary,
            ContextMenu = new ContextMenu { ActionGroups = [new ActionGroup { Nodes = options.ToList() }] }
        });

    /// <summary>A plain one-click toolbar button.</summary>
    public static void AddButton(ScreenLayout layout, string label, Icon icon, ActionBase action) =>
        layout.AddAction(new Button
        {
            Name = label,
            Icon = icon,
            Type = Button.ButtonType.Secondary,
            NodeAction = action
        });

    /// <summary>
    /// A toolbar button that opens DW's searchable slide-over selector and, on pick, stores
    /// the selection under a render-time token and navigates to <paramref name="onPicked"/>
    /// (whose query must carry the same token) — see <c>PickStore</c>.
    /// </summary>
    public static void AddPicker(
        Dynamicweb.CoreUI.Layout.ScreenLayout layout,
        string label,
        Icon icon,
        Dynamicweb.CoreUI.Editors.Selectors.SelectorProviderBase provider,
        string token,
        Dynamicweb.CoreUI.Actions.Implementations.NavigateScreenAction onPicked) =>
        AddButton(layout, label, icon,
            Dynamicweb.CoreUI.Actions.Implementations.OpenSlideOverAction
                .To<Dynamicweb.Application.UI.Screens.SelectorScreen>()
                .With(new Dynamicweb.Application.UI.Queries.SelectorDataByProviderQuery(provider))
                .WithOnSelectAction(
                    Dynamicweb.CoreUI.Actions.Implementations.RunCommandAction
                        .For(new Commands.ToolbarPickCommand { Token = token })
                        .WithCommandProperty(nameof(Commands.ToolbarPickCommand.PickedId))
                        .WithOnSuccess(onPicked.WithForceReload())));

    public static ActionNode Option(string name, bool active, ActionBase action) => new()
    {
        Name = name,
        Icon = active ? Icon.Check : null,
        NodeAction = action
    };
}
