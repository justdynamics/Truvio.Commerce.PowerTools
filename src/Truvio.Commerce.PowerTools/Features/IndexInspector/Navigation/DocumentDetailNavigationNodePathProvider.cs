using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Navigation;

public sealed class DocumentDetailNavigationNodePathProvider : NavigationNodePathProvider<DocumentDetailModel>
{
    public DocumentDetailNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(DocumentDetailModel? model) =>
        PowerToolsNavigationPaths.For<SearchSection>(SearchNodeProvider.DocumentsNodeId);
}
