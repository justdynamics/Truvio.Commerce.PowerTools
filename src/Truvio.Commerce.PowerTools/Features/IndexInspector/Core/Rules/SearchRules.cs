using System.Globalization;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

namespace Truvio.Commerce.PowerTools.Features.IndexInspector.Core.Rules;

/// <summary>The rule set the linter runs, in rule-id order.</summary>
public static class SearchRules
{
    public static IReadOnlyList<IQueryLintRule> All() =>
    [
        new BlankParameterClauseRule(),
        new QueryMatchesEverythingRule(),
        new UndeclaredParameterRule(),
        new UnusedParameterRule(),
        new DisabledClauseRule(),
        new MissingQuerySourceRule(),
        new MissingExpressionFieldRule(),
        new MissingSortFieldRule(),
        new UnsortableFieldRule(),
        new MissingFacetSourceRule(),
        new MissingFacetFieldRule(),
        new UnindexedFacetFieldRule(),
        new AnalyzedFacetFieldRule(),
        new FacetParameterRule(),
        new DuplicateQueryRule(),
        new UnusedIndexRule(),
        new IndexNotBuiltRule()
    ];
}
