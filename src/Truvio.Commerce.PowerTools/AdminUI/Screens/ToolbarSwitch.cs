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

    public static ActionNode Option(string name, bool active, ActionBase action) => new()
    {
        Name = name,
        Icon = active ? Icon.Check : null,
        NodeAction = action
    };
}
