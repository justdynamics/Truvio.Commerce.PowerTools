using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Data;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.Features.QueryTester.Commands;
using Truvio.Commerce.PowerTools.Features.QueryTester.Models;
using Truvio.Commerce.PowerTools.Features.QueryTester.Queries;
using Truvio.Commerce.PowerTools.Shared.Security;

namespace Truvio.Commerce.PowerTools.Features.QueryTester.Screens;

/// <summary>
/// Step 2, optional: the values for the run — a dialog with one text input per declared
/// parameter (a prompt screen, the only CoreUI screen kind whose OK posts edited values back
/// to a command). OK saves the set as the user's draft and opens the report with
/// <c>UseDraft=true</c>; the report then renders every link with the resolved values, so
/// shareability is kept.
/// </summary>
public sealed class QueryValuesScreen : PromptScreenBase<QueryValuesModel>
{
    private QueryValuesQuery Q => Query as QueryValuesQuery ?? new QueryValuesQuery();

    protected override string GetScreenName() =>
        string.IsNullOrEmpty(Model?.QueryName) ? "Set parameters" : $"Set parameters: {Model.QueryName}";

    protected override void BuildPromptScreen() => AddDynamicFields(m => m.Fields);

    protected override string GetOkActionName() => "Run the query";

    /// <summary>Null when the user may not run queries: CoreUI then renders no OK button.</summary>
    protected override CommandBase<QueryValuesModel>? GetOkCommand() =>
        PowerToolsAccess.CanUseSearchInspector()
            ? new QueryValuesRunCommand { Repository = Q.Repository, Item = Q.Item }
            : null;

    /// <summary>
    /// OK saves the draft, then opens the report reading it. ForceReload matters: rerunning
    /// from a report that is already at the <c>UseDraft</c> URL navigates to the same URL,
    /// which the client otherwise treats as a no-op and the old report stays on screen.
    /// </summary>
    protected override RunCommandAction ConfigureOkAction(RunCommandAction action) =>
        action.WithOnSuccess(NavigateScreenAction.To<QueryTestScreen>()
            .With(new QueryTestQuery { Repository = Q.Repository, Item = Q.Item, UseDraft = true })
            .WithForceReload()
            .UpdateParameters(p => p.Replace = true));
}
