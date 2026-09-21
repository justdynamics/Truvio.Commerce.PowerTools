namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Models;

/// <summary>
/// A single line in one of the Operations report tables: a label, an optional badge, a value
/// and an explanation. The same shape the Price Explainer report uses, so the two tools read
/// the same way.
/// </summary>
public sealed class OpsRowModel
{
    public string Item { get; set; } = string.Empty;

    public string Verdict { get; set; } = string.Empty;

    /// <summary>win / ok / match / reject / warn / info — drives the badge colour.</summary>
    public string VerdictKind { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string Why { get; set; } = string.Empty;
}
