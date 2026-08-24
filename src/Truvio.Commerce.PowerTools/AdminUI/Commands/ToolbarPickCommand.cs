using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Truvio.Commerce.PowerTools.AdminUI.Security;

namespace Truvio.Commerce.PowerTools.AdminUI.Commands;

/// <summary>
/// Receives a slide-over selection (the client writes the picked id into
/// <see cref="PickedId"/> via the action's <c>CommandProperty</c>) and stores it under the
/// render-time token, for the navigation that follows — see <see cref="PickStore"/>.
/// Writes nothing to Dynamicweb.
/// </summary>
public sealed class ToolbarPickCommand : CommandBase
{
    public string Token { get; set; } = string.Empty;

    public string PickedId { get; set; } = string.Empty;

    public override CommandResult Handle()
    {
        if (!PowerToolsAccess.CanViewSettings())
            return new CommandResult { Status = CommandResult.ResultType.NotAllowed };

        if (string.IsNullOrEmpty(Token) || string.IsNullOrEmpty(PickedId))
            return new CommandResult { Status = CommandResult.ResultType.Invalid, Message = "Nothing was selected." };

        PickStore.Set(Token, PickedId);
        return new CommandResult { Status = CommandResult.ResultType.Ok };
    }
}
