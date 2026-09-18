using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Navigation;

public sealed class FieldUsageNavigationNodePathProvider : NavigationNodePathProvider<FieldUsageModel>
{
    public FieldUsageNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(FieldUsageModel? model) =>
        PowerToolsNavigationPaths.For<SearchSection>(SearchNodeProvider.FieldUsageNodeId);
}
