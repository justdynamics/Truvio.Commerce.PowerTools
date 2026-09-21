namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

/// <summary>How a field is referenced from outside the schema.</summary>
public enum FieldUsageKind
{
    Expression,
    Sort,
    Facet
}
