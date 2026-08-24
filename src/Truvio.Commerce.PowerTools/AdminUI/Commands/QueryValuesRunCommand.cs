using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Truvio.Commerce.PowerTools.AdminUI.Security;

namespace Truvio.Commerce.PowerTools.AdminUI.Commands;

/// <summary>
/// OK command of the "Set parameters" dialog. The client posts the dialog's query alongside,
/// the model builder rebuilds <c>Model.Fields</c> from it and merges the typed values in, so
/// this just flattens fields back to <c>name=value;...</c> and stores it as the user's draft
/// for this query. It writes nothing to Dynamicweb — the tester stays read-only; the
/// navigation that follows (wired in the screen) opens the report with <c>UseDraft=true</c>.
/// </summary>
public sealed class QueryValuesRunCommand : CommandBase<QueryValuesModel>
{
    public string Repository { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    public override CommandResult Handle()
    {
        if (!PowerToolsAccess.CanUseSearchInspector())
            return new CommandResult { Status = CommandResult.ResultType.NotAllowed, Message = "You need Read on the search inspector." };

        if (string.IsNullOrEmpty(Repository) || string.IsNullOrEmpty(Item))
            return new CommandResult { Status = CommandResult.ResultType.Invalid, Message = "No query selected." };

        var values = (Model?.Fields.Groups ?? [])
            .SelectMany(group => group.Fields)
            .Select(field => new KeyValuePair<string, string>(field.SystemName, Convert.ToString(field.Value) ?? string.Empty))
            .Where(pair => !string.IsNullOrEmpty(pair.Key) && !string.IsNullOrEmpty(pair.Value));

        ParameterDraftStore.Set(Repository, Item, Core.Search.Testing.ParameterValues.Format(values));

        return new CommandResult { Status = CommandResult.ResultType.Ok };
    }
}
