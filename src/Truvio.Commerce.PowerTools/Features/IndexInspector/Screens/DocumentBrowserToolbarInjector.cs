using Dynamicweb.CoreUI.Screens;
using Icon = Dynamicweb.CoreUI.Icons.Icon;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Queries;
using Truvio.Commerce.PowerTools.Shared.AdminUI;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Screens;

/// <summary>
/// Document count and database comparison as toolbar controls, labelled with the state in
/// effect - see <see cref="ToolbarSwitch"/>.
/// </summary>
public sealed class DocumentBrowserToolbarInjector : ScreenInjector<DocumentBrowserScreen>
{
    public override void OnAfter(DocumentBrowserScreen screen, Dynamicweb.CoreUI.UiComponentBase content)
    {
        if (content is not Dynamicweb.CoreUI.Layout.ScreenLayout layout)
            return;

        if (screen.Query is not DocumentBrowserQuery q || string.IsNullOrEmpty(q.Repository) || string.IsNullOrEmpty(q.Item))
            return;

        ToolbarSwitch.Add(layout, $"{q.Take} docs", Icon.ListUl,
            DocumentBrowserScreen.TakePresets.Select(take => ToolbarSwitch.Option($"Show {take} documents",
                active: take == q.Take, DocumentBrowserScreen.Navigate(q, x => x.Take = take))));

        if (DocumentBrowserQuery.IsProductIndex(q.Repository, q.Item))
        {
            ToolbarSwitch.Add(layout, q.Compare ? "Compare on" : "Compare off", Icon.Balance,
            [
                ToolbarSwitch.Option("Compare with the database", active: q.Compare, DocumentBrowserScreen.Navigate(q, x => x.Compare = true)),
                ToolbarSwitch.Option("Stop comparing with the database", active: !q.Compare, DocumentBrowserScreen.Navigate(q, x => x.Compare = false))
            ]);
        }
    }
}
