using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.AdminUI.Models;

namespace Truvio.Commerce.PowerTools.AdminUI.Tree;

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

// ---- Commerce section --------------------------------------------------------------------

public sealed class ExplainerAccountListNavigationNodePathProvider : NavigationNodePathProvider<ExplainerAccountModel>
{
    public ExplainerAccountListNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(ExplainerAccountModel? model) =>
        PowerToolsNavigationPaths.For<CommerceSection>(CommerceNodeProvider.PriceExplainerNodeId);
}

public sealed class ProductPickNavigationNodePathProvider : NavigationNodePathProvider<ProductPickModel>
{
    public ProductPickNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(ProductPickModel? model) =>
        PowerToolsNavigationPaths.For<CommerceSection>(CommerceNodeProvider.PriceExplainerNodeId);
}

public sealed class PriceExplainNavigationNodePathProvider : NavigationNodePathProvider<PriceExplainModel>
{
    public PriceExplainNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(PriceExplainModel? model) =>
        PowerToolsNavigationPaths.For<CommerceSection>(CommerceNodeProvider.PriceExplainerNodeId);
}

/// <summary>Anchors the account picker (and its drilldowns) under PowerTools > Security > Security Viewer.</summary>
public sealed class AccountListNavigationNodePathProvider : NavigationNodePathProvider<AccountListModel>
{
    public AccountListNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(AccountListModel? model) =>
        PowerToolsNavigationPaths.For(SecurityNodeProvider.SecurityViewerNodeId);
}

public sealed class AccessNodeNavigationNodePathProvider : NavigationNodePathProvider<AccessNodeModel>
{
    public AccessNodeNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(AccessNodeModel? model) =>
        PowerToolsNavigationPaths.For(SecurityNodeProvider.SecurityViewerNodeId);
}

public sealed class AudienceItemNavigationNodePathProvider : NavigationNodePathProvider<AudienceItemModel>
{
    public AudienceItemNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(AudienceItemModel? model) =>
        PowerToolsNavigationPaths.For(SecurityNodeProvider.SecurityViewerNodeId);
}

public sealed class FindingNavigationNodePathProvider : NavigationNodePathProvider<FindingModel>
{
    public FindingNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(FindingModel? model) =>
        PowerToolsNavigationPaths.For(SecurityNodeProvider.WarningsNodeId);
}
