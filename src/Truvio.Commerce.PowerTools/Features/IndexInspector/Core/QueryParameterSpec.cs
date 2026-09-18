namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

public sealed record QueryParameterSpec(string Name, string TypeName, string DefaultValue)
{
    /// <summary>
    /// A parameter only ever reaches the index provider when it has a non-empty default (or
    /// an explicit runtime value) — see <c>LuceneQueryProvider.HandleParameters</c>.
    /// </summary>
    public bool HasDefault => !string.IsNullOrEmpty(DefaultValue);
}
