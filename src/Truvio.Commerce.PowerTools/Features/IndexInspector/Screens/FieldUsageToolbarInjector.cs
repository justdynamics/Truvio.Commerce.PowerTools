using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Icons;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Queries;
using Truvio.Commerce.PowerTools.Shared.AdminUI;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Screens;

/// <summary>The dangling/unused filter as a toolbar control labelled with the view in effect.</summary>
public sealed class FieldUsageToolbarInjector : ScreenInjector<FieldUsageScreen>
{
    public override void OnAfter(FieldUsageScreen screen, Dynamicweb.CoreUI.UiComponentBase content)
    {
        if (content is not Dynamicweb.CoreUI.Layout.ScreenLayout layout)
            return;

        var problemsOnly = (screen.Query as FieldUsageQuery)?.ProblemsOnly ?? false;

        ToolbarSwitch.Add(layout, problemsOnly ? "Problems only" : "All fields", Icon.Filter,
        [
            ToolbarSwitch.Option("All fields", active: !problemsOnly,
                NavigateScreenAction.To<FieldUsageScreen>().With(new FieldUsageQuery())),
            ToolbarSwitch.Option("Only dangling and unused", active: problemsOnly,
                NavigateScreenAction.To<FieldUsageScreen>().With(new FieldUsageQuery { ProblemsOnly = true }))
        ]);
    }
}
