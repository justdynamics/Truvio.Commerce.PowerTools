namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Core;

/// <summary>One "who changed what" entry.</summary>
/// <param name="When">Timestamp of the change.</param>
/// <param name="Who">User name, or "unknown" when the source keeps no attribution.</param>
/// <param name="What">What was changed (command/type + name).</param>
/// <param name="Where">Which area/source the entry came from.</param>
public sealed record ChangeSpec(DateTime When, string Who, string What, string Where);
