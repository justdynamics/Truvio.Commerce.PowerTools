using Dynamicweb.Content.UI;
using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerApps.SecOps.AdminUI.Models;

namespace Truvio.Commerce.PowerApps.SecOps.AdminUI.Tree;

internal static class SecOpsNavigationPaths
{
    /// <summary>
    /// The Content tree's built-in node providers parse the navigation context as an AreaId
    /// and throw on <see cref="NavigationContext.Empty"/> ('--') while resolving breadcrumbs,
    /// so the path must always carry a real area id.
    /// </summary>
    private static string AreaContext()
    {
        try
        {
            return (Dynamicweb.Content.Services.Areas.GetAreas().FirstOrDefault()?.ID ?? 1).ToString();
        }
        catch
        {
            return "1";
        }
    }

    public static NavigationNodePath For(string nodeId) =>
        new([
            typeof(ContentArea).FullName!,
            AreaContext(),
            typeof(ToolsSection).FullName!,
            nodeId
        ]);
}

/// <summary>Anchors the account picker (and its drilldowns) under Content > SecOps > Security Viewer.</summary>
public sealed class AccountListNavigationNodePathProvider : NavigationNodePathProvider<AccountListModel>
{
    public AccountListNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(AccountListModel? model) =>
        SecOpsNavigationPaths.For(SecOpsNodeProvider.SecurityViewerNodeId);
}

public sealed class AccessNodeNavigationNodePathProvider : NavigationNodePathProvider<AccessNodeModel>
{
    public AccessNodeNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(AccessNodeModel? model) =>
        SecOpsNavigationPaths.For(SecOpsNodeProvider.SecurityViewerNodeId);
}

public sealed class AudienceItemNavigationNodePathProvider : NavigationNodePathProvider<AudienceItemModel>
{
    public AudienceItemNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(AudienceItemModel? model) =>
        SecOpsNavigationPaths.For(SecOpsNodeProvider.SecurityViewerNodeId);
}

public sealed class FindingNavigationNodePathProvider : NavigationNodePathProvider<FindingModel>
{
    public FindingNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(FindingModel? model) =>
        SecOpsNavigationPaths.For(SecOpsNodeProvider.WarningsNodeId);
}
