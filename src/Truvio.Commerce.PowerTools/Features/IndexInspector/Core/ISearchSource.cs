namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

/// <summary>Reads the repository definitions. The DW adapter is the only implementation.</summary>
public interface ISearchSource
{
    IReadOnlyList<RepositorySpec> GetRepositories();
}
