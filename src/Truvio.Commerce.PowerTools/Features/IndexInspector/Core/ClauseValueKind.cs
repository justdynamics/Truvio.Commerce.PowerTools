namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

/// <summary>What sits on the right-hand side of a binary clause.</summary>
public enum ClauseValueKind
{
    Constant,
    Term,
    Parameter,
    Macro,
    Code,
    Unknown
}
