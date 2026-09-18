namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Dw;

/// <summary>How an indexed product document compares with the product row in the database.</summary>
public enum ProductMatch
{
    NotAProduct,
    Match,
    Differs,
    MissingInDatabase,
    Unknown
}
