namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

public sealed record QueryClauseSpec(
    string Path,
    string FieldName,
    string Operator,
    ClauseValueKind ValueKind,
    string ParameterName,
    string Value,
    bool Disabled) : QueryNodeSpec(Path)
{
    public override string ToString() =>
        $"{FieldName} {Operator} {(ValueKind == ClauseValueKind.Parameter ? "@" + ParameterName : Value)}";
}
