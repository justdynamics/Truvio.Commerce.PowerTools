using Dynamicweb.CoreUI.Navigation;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Models;
using Truvio.Commerce.PowerTools.Shared.Navigation;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Navigation;

public sealed class DocumentRowNavigationNodePathProvider : NavigationNodePathProvider<DocumentRowModel>
{
    public DocumentRowNavigationNodePathProvider() => AllowNullModel = true;

    protected override NavigationNodePath GetNavigationNodePathInternal(DocumentRowModel? model) =>
        PowerToolsNavigationPaths.For<SearchSection>(SearchNodeProvider.DocumentsNodeId);
}
