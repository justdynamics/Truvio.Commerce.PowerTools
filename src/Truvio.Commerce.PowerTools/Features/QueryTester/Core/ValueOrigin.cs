namespace Truvio.Commerce.PowerTools.Features.QueryTester.Core;

/// <summary>Where the value on the right-hand side of a clause came from for this run.</summary>
public enum ValueOrigin
{
    None,
    SuppliedValue,
    ParameterDefault,
    MissingParameter,
    UndeclaredParameter,
    Constant,
    Term,
    Macro,
    Code,
    FullText
}
