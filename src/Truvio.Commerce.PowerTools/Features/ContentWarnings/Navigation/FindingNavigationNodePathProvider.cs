using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.ContentWarnings.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.ContentWarnings.Navigation;

public sealed class FindingNavigationNodePathProvider : NavigationNodePathProvider<FindingModel>
{
    public FindingNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(FindingModel? model) =>
        PowerToolsNavigationPaths.For(SecurityNodeProvider.WarningsNodeId);
}
