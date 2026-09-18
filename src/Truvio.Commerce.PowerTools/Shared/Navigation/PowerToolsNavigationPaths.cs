using Dynamicweb.CoreUI.Navigation;

namespace Truvio.Commerce.PowerTools.Shared.Navigation;

internal static class PowerToolsNavigationPaths
{
    /// <summary>
    /// The PowerTools area has no sub-areas, so the context segment is
    /// <see cref="NavigationContext.Empty"/> — the same convention the Settings area uses.
    /// Only this assembly's providers resolve paths under the area.
    /// </summary>
    public static NavigationNodePath For(string nodeId) => For<SecuritySection>(nodeId);

    public static NavigationNodePath For<TSection>(string nodeId) =>
        new([
            typeof(PowerToolsArea).FullName!,
            NavigationContext.Empty,
            typeof(TSection).FullName!,
            nodeId
        ]);
}
