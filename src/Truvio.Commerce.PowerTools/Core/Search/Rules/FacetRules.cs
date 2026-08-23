using Truvio.Commerce.PowerTools.Core.Diagnostics;

namespace Truvio.Commerce.PowerTools.Core.Search.Rules;

/// <summary>IDX-W10 — the facet group's source query does not exist.</summary>
public sealed class MissingFacetSourceRule : IQueryLintRule
{
    public string RuleId => "IDX-W10";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var group in catalog.FacetGroups)
        {
            if (catalog.Query(group.SourceKey) is not null)
                continue;

            var source = string.IsNullOrEmpty(group.SourceKey) ? "(none)" : group.SourceKey;

            yield return new Finding(
                RuleId,
                FindingSeverity.Warning,
                SearchEntityNames.Facets,
                group.Key,
                catalog.Describe(group),
                $"Source query '{source}' does not exist",
                "The facet group points at a query that is not in any repository, so it can never be filled.");
        }
    }
}

/// <summary>
/// IDX-W11 — a facet on a field the index schema does not have. <c>DoFacetSearch</c> looks the
/// field up in the schema and silently skips the facet when it is not found, so the facet is
/// simply never rendered.
/// </summary>
public sealed class MissingFacetFieldRule : IQueryLintRule
{
    public string RuleId => "IDX-W11";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var group in catalog.FacetGroups)
        {
            var index = catalog.IndexFor(group);
            if (index is null)
                continue;

            foreach (var facet in group.Facets)
            {
                if (string.IsNullOrEmpty(facet.Field) || index.Field(facet.Field) is not null)
                    continue;

                yield return new Finding(
                    RuleId,
                    FindingSeverity.Warning,
                    SearchEntityNames.Facets,
                    group.Key,
                    catalog.Describe(group),
                    $"Facet '{facet.Name}' uses field '{facet.Field}', which is not in the index schema",
                    $"Index '{index.Name}' has no field with that system name, so the facet is skipped and " +
                    "never appears in the result.");
            }
        }
    }
}

/// <summary>
/// IDX-W12 — a facet on a field with <c>Indexed=false</c>. The index writer only emits the
/// facet doc-values (<c>AddFacetFields</c>) when the field definition is indexed, so the
/// facet always comes back empty.
/// </summary>
public sealed class UnindexedFacetFieldRule : IQueryLintRule
{
    public string RuleId => "IDX-W12";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var group in catalog.FacetGroups)
        {
            var index = catalog.IndexFor(group);
            if (index is null)
                continue;

            foreach (var facet in group.Facets)
            {
                var field = index.Field(facet.Field);
                if (field is null || field.Indexed)
                    continue;

                yield return new Finding(
                    RuleId,
                    FindingSeverity.Warning,
                    SearchEntityNames.Facets,
                    group.Key,
                    catalog.Describe(group),
                    $"Facet '{facet.Name}' uses field '{facet.Field}', which is not indexed",
                    "Facet values are only written for indexed fields, so this facet always comes back empty. " +
                    "Set Indexed on the field and rebuild the index.");
            }
        }
    }
}

/// <summary>
/// IDX-W13 — a facet on an analyzed field. Facet values are written per analyzer token for an
/// analyzed field, so the buckets are single words instead of the whole field value
/// ("Bosch Professional" becomes "bosch" + "professional").
/// </summary>
public sealed class AnalyzedFacetFieldRule : IQueryLintRule
{
    public string RuleId => "IDX-W13";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var group in catalog.FacetGroups)
        {
            var index = catalog.IndexFor(group);
            if (index is null)
                continue;

            foreach (var facet in group.Facets)
            {
                var field = index.Field(facet.Field);
                if (field is null || !field.Indexed || !field.Analyzed)
                    continue;

                yield return new Finding(
                    RuleId,
                    FindingSeverity.Info,
                    SearchEntityNames.Facets,
                    group.Key,
                    catalog.Describe(group),
                    $"Facet '{facet.Name}' uses analyzed field '{facet.Field}'",
                    "For an analyzed field the facet buckets are analyzer tokens, not whole values. " +
                    "Facet on a non-analyzed copy of the field if you want one bucket per value.");
            }
        }
    }
}

/// <summary>
/// IDX-W14 — a facet whose query parameter is missing or is not declared on the source query.
/// <c>DoFacetSearch</c> skips a facet with a null query parameter outright, and a selection on
/// a parameter the query does not declare never reaches the expression.
/// </summary>
public sealed class FacetParameterRule : IQueryLintRule
{
    public string RuleId => "IDX-W14";

    public IEnumerable<Finding> Evaluate(SearchCatalog catalog)
    {
        foreach (var group in catalog.FacetGroups)
        {
            var query = catalog.Query(group.SourceKey);
            if (query is null)
                continue;

            foreach (var facet in group.Facets)
            {
                if (string.IsNullOrEmpty(facet.QueryParameter))
                {
                    yield return new Finding(
                        RuleId,
                        FindingSeverity.Warning,
                        SearchEntityNames.Facets,
                        group.Key,
                        catalog.Describe(group),
                        $"Facet '{facet.Name}' has no query parameter",
                        "A facet without a query parameter is skipped when the facets are built, so it never renders.");
                    continue;
                }

                if (query.Parameter(facet.QueryParameter) is not null)
                    continue;

                yield return new Finding(
                    RuleId,
                    FindingSeverity.Warning,
                    SearchEntityNames.Facets,
                    group.Key,
                    catalog.Describe(group),
                    $"Facet '{facet.Name}' filters on parameter '{facet.QueryParameter}', which query '{query.Name}' does not declare",
                    "Selecting a value in this facet cannot narrow the result: the query never receives the parameter.");
            }
        }
    }
}
