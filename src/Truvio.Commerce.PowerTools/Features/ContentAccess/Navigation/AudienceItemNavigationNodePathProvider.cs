using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.ContentAccess.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.ContentAccess.Navigation;

public sealed class AudienceItemNavigationNodePathProvider : NavigationNodePathProvider<AudienceItemModel>
{
    public AudienceItemNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(AudienceItemModel? model) =>
        PowerToolsNavigationPaths.For(SecurityNodeProvider.SecurityViewerNodeId);
}
