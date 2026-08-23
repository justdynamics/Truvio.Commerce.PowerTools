using Truvio.Commerce.PowerTools.Core.Diagnostics;
using Truvio.Commerce.PowerTools.Core.Search;
using Truvio.Commerce.PowerTools.Core.Search.Rules;
using Xunit;
using static Truvio.Commerce.PowerTools.Tests.SearchSpecBuilders;

namespace Truvio.Commerce.PowerTools.Tests;

public class QueryLintRuleTests
{
    private static IReadOnlyList<Finding> Run(IQueryLintRule rule, SearchCatalog catalog) =>
        rule.Evaluate(catalog).ToList();

    // ---- IDX-W1 blank parameter ------------------------------------------------------

    [Fact]
    public void BlankParameter_flags_a_clause_whose_parameter_has_no_default()
    {
        var query = Query(
            And(Clause("Name"), ParameterClause("Active", "active")),
            [Parameter("active")]);

        var findings = Run(new BlankParameterClauseRule(), Catalog(queries: [query]));

        var finding = Assert.Single(findings);
        Assert.Equal("IDX-W1", finding.RuleId);
        Assert.Equal(FindingSeverity.Warning, finding.Severity);
        Assert.Contains("active", finding.Title);
        Assert.Contains("MORE documents", finding.Detail);
    }

    [Fact]
    public void BlankParameter_is_quiet_when_the_parameter_has_a_default()
    {
        var query = Query(
            And(Clause("Name"), ParameterClause("Active", "active")),
            [Parameter("active", "True")]);

        Assert.Empty(Run(new BlankParameterClauseRule(), Catalog(queries: [query])));
    }

    [Fact]
    public void BlankParameter_says_FEWER_documents_inside_an_Or_group()
    {
        var query = Query(
            Or(Clause("Name"), ParameterClause("Active", "active")),
            [Parameter("active")]);

        var finding = Assert.Single(Run(new BlankParameterClauseRule(), Catalog(queries: [query])));
        Assert.Contains("FEWER documents", finding.Detail);
    }

    [Fact]
    public void BlankParameter_leaves_IsEmpty_alone_because_a_null_value_still_executes()
    {
        var query = Query(
            And(Clause("Name"), ParameterClause("Active", "active", op: "IsEmpty")),
            [Parameter("active")]);

        Assert.Empty(Run(new BlankParameterClauseRule(), Catalog(queries: [query])));
    }

    // ---- IDX-W2 match-all collapse ---------------------------------------------------

    [Fact]
    public void MatchesEverything_fires_when_every_clause_can_disappear()
    {
        var query = Query(
            And(ParameterClause("Name", "q"), ParameterClause("Active", "active")),
            [Parameter("q"), Parameter("active")]);

        var finding = Assert.Single(Run(new QueryMatchesEverythingRule(), Catalog(queries: [query])));
        Assert.Equal("IDX-W2", finding.RuleId);
        Assert.Equal(FindingSeverity.Critical, finding.Severity);
    }

    [Fact]
    public void MatchesEverything_fires_for_a_query_with_no_expression()
    {
        var finding = Assert.Single(Run(new QueryMatchesEverythingRule(), Catalog(queries: [Query(null)])));
        Assert.Contains("no expression", finding.Detail);
    }

    [Fact]
    public void MatchesEverything_is_quiet_when_one_constant_clause_survives()
    {
        var query = Query(
            And(Clause("Active"), ParameterClause("Name", "q")),
            [Parameter("q")]);

        Assert.Empty(Run(new QueryMatchesEverythingRule(), Catalog(queries: [query])));
    }

    [Fact]
    public void MatchesEverything_counts_a_disabled_clause_as_gone()
    {
        var query = Query(And(Clause("Active", disabled: true)));

        Assert.Single(Run(new QueryMatchesEverythingRule(), Catalog(queries: [query])));
    }

    [Fact]
    public void BlankParameter_defers_to_the_match_all_rule()
    {
        var query = Query(And(ParameterClause("Name", "q")), [Parameter("q")]);

        Assert.Empty(Run(new BlankParameterClauseRule(), Catalog(queries: [query])));
        Assert.Single(Run(new QueryMatchesEverythingRule(), Catalog(queries: [query])));
    }

    // ---- IDX-W3/W4/W5 parameters -----------------------------------------------------

    [Fact]
    public void UndeclaredParameter_is_critical()
    {
        var query = Query(And(Clause("Active"), ParameterClause("Name", "ghost")));

        var finding = Assert.Single(Run(new UndeclaredParameterRule(), Catalog(queries: [query])));
        Assert.Equal(FindingSeverity.Critical, finding.Severity);
        Assert.Contains("ghost", finding.Title);
    }

    [Fact]
    public void UnusedParameter_is_quiet_when_a_facet_uses_it()
    {
        var query = Query(And(Clause("Active")), [Parameter("Manufacturer")]);
        var facets = Facets([Facet("Manufacturer", "Name", "Manufacturer")]);

        Assert.Empty(Run(new UnusedParameterRule(), Catalog(queries: [query], facetGroups: [facets])));
    }

    [Fact]
    public void UnusedParameter_flags_a_parameter_nothing_references()
    {
        var query = Query(And(Clause("Active")), [Parameter("orphan")]);

        var finding = Assert.Single(Run(new UnusedParameterRule(), Catalog(queries: [query])));
        Assert.Contains("orphan", finding.Title);
    }

    [Fact]
    public void DisabledClause_is_reported_as_information()
    {
        var query = Query(And(Clause("Active"), Clause("Name", disabled: true)));

        var finding = Assert.Single(Run(new DisabledClauseRule(), Catalog(queries: [query])));
        Assert.Equal(FindingSeverity.Info, finding.Severity);
    }

    // ---- IDX-W6..W9 field references -------------------------------------------------

    [Fact]
    public void MissingQuerySource_fires_when_the_index_does_not_exist()
    {
        var query = Query(And(Clause("Active")), sourceItem: "Gone.index");

        var finding = Assert.Single(Run(new MissingQuerySourceRule(), Catalog(queries: [query])));
        Assert.Equal(FindingSeverity.Critical, finding.Severity);
    }

    [Fact]
    public void MissingExpressionField_fires_for_a_field_the_schema_does_not_have()
    {
        var query = Query(And(Clause("Nope")));

        var finding = Assert.Single(Run(new MissingExpressionFieldRule(), Catalog(queries: [query])));
        Assert.Equal(FindingSeverity.Critical, finding.Severity);
        Assert.Contains("Nope", finding.Title);
    }

    [Fact]
    public void MissingExpressionField_ignores_a_disabled_clause()
    {
        var query = Query(And(Clause("Nope", disabled: true)));

        Assert.Empty(Run(new MissingExpressionFieldRule(), Catalog(queries: [query])));
    }

    [Fact]
    public void MissingSortField_fires_but_leaves_score_alone()
    {
        var query = Query(
            And(Clause("Active")),
            sortOrder: [new QuerySortSpec("_score", "Descending"), new QuerySortSpec("Gone", "Ascending")]);

        var finding = Assert.Single(Run(new MissingSortFieldRule(), Catalog(queries: [query])));
        Assert.Contains("Gone", finding.Title);
        Assert.Equal(FindingSeverity.Warning, finding.Severity);
    }

    [Fact]
    public void UnsortableField_fires_for_a_stored_only_field()
    {
        var index = Index([Field("Active", "System.Boolean"), Field("Blob", indexed: false)]);
        var query = Query(And(Clause("Active")), sortOrder: [new QuerySortSpec("Blob", "Ascending")]);

        var finding = Assert.Single(Run(new UnsortableFieldRule(), Catalog([index], [query])));
        Assert.Contains("not indexed", finding.Title);
    }

    // ---- IDX-W10..W14 facets ---------------------------------------------------------

    [Fact]
    public void MissingFacetSource_fires_when_the_query_does_not_exist()
    {
        var facets = Facets([Facet("Brand", "Name", "brand")], sourceItem: "Gone.query");

        Assert.Single(Run(new MissingFacetSourceRule(), Catalog(facetGroups: [facets])));
    }

    [Fact]
    public void MissingFacetField_fires_for_a_field_outside_the_schema()
    {
        var query = Query(And(Clause("Active")), [Parameter("brand")]);
        var facets = Facets([Facet("Brand", "Ghost", "brand")]);

        var finding = Assert.Single(Run(new MissingFacetFieldRule(), Catalog(queries: [query], facetGroups: [facets])));
        Assert.Contains("Ghost", finding.Title);
    }

    [Fact]
    public void UnindexedFacetField_fires_for_a_stored_only_field()
    {
        var index = Index([Field("Active", "System.Boolean"), Field("Blob", indexed: false)]);
        var query = Query(And(Clause("Active")), [Parameter("blob")]);
        var facets = Facets([Facet("Blob", "Blob", "blob")]);

        var finding = Assert.Single(Run(new UnindexedFacetFieldRule(), Catalog([index], [query], [facets])));
        Assert.Contains("not indexed", finding.Title);
    }

    [Fact]
    public void AnalyzedFacetField_is_information_only()
    {
        var index = Index([Field("Brand", analyzed: true)]);
        var query = Query(And(Clause("Brand")), [Parameter("brand")]);
        var facets = Facets([Facet("Brand", "Brand", "brand")]);

        var finding = Assert.Single(Run(new AnalyzedFacetFieldRule(), Catalog([index], [query], [facets])));
        Assert.Equal(FindingSeverity.Info, finding.Severity);
    }

    [Fact]
    public void FacetParameter_flags_a_missing_and_an_undeclared_parameter()
    {
        var query = Query(And(Clause("Name")), [Parameter("known")]);
        var facets = Facets(
        [
            Facet("NoParameter", "Name", string.Empty),
            Facet("Unknown", "Name", "unknown"),
            Facet("Fine", "Name", "known")
        ]);

        var findings = Run(new FacetParameterRule(), Catalog(queries: [query], facetGroups: [facets]));

        Assert.Equal(2, findings.Count);
        Assert.Contains(findings, f => f.Title.Contains("no query parameter"));
        Assert.Contains(findings, f => f.Title.Contains("unknown"));
    }

    // ---- IDX-W15..W17 catalog health -------------------------------------------------

    [Fact]
    public void DuplicateQuery_reports_both_members()
    {
        var a = Query(And(Clause("Active")), item: "A.query");
        var b = Query(And(Clause("Active")), item: "B.query");

        var findings = Run(new DuplicateQueryRule(), Catalog(queries: [a, b]));

        Assert.Equal(2, findings.Count);
        Assert.All(findings, f => Assert.Equal(FindingSeverity.Info, f.Severity));
    }

    [Fact]
    public void DuplicateQuery_ignores_queries_that_differ_in_sort_order()
    {
        var a = Query(And(Clause("Active")), item: "A.query", sortOrder: [new QuerySortSpec("Name", "Ascending")]);
        var b = Query(And(Clause("Active")), item: "B.query");

        Assert.Empty(Run(new DuplicateQueryRule(), Catalog(queries: [a, b])));
    }

    [Fact]
    public void UnusedIndex_fires_when_no_query_points_at_it()
    {
        Assert.Single(Run(new UnusedIndexRule(), Catalog()));
    }

    [Fact]
    public void UnusedIndex_is_quiet_when_a_query_reads_from_it()
    {
        Assert.Empty(Run(new UnusedIndexRule(), Catalog(queries: [Query(And(Clause("Active")))])));
    }

    [Theory]
    [InlineData(IndexHealth.NeverBuilt, FindingSeverity.Critical)]
    [InlineData(IndexHealth.Failed, FindingSeverity.Critical)]
    [InlineData(IndexHealth.Stale, FindingSeverity.Warning)]
    public void IndexNotBuilt_maps_health_onto_severity(IndexHealth health, FindingSeverity expected)
    {
        var finding = Assert.Single(Run(new IndexNotBuiltRule(), Catalog([Index(health: health)])));
        Assert.Equal(expected, finding.Severity);
    }

    [Fact]
    public void IndexNotBuilt_is_quiet_for_a_healthy_index()
    {
        Assert.Empty(Run(new IndexNotBuiltRule(), Catalog([Index(health: IndexHealth.Ok)])));
    }

    // ---- engine ----------------------------------------------------------------------

    [Fact]
    public void Engine_orders_critical_findings_first()
    {
        var query = Query(
            And(Clause("Active"), ParameterClause("Name", "ghost"), Clause("Name", disabled: true)),
            [Parameter("orphan")]);

        var findings = new QueryLintEngine().Run(Catalog(queries: [query]));

        Assert.NotEmpty(findings);
        Assert.Equal(FindingSeverity.Critical, findings[0].Severity);
        Assert.Equal(FindingSeverity.Info, findings[^1].Severity);
    }

    [Fact]
    public void Engine_survives_a_rule_that_throws()
    {
        var findings = new QueryLintEngine([new ThrowingRule()]).Run(Catalog());

        var finding = Assert.Single(findings);
        Assert.Equal("IDX-TEST", finding.RuleId);
        Assert.Equal("Rule could not be evaluated", finding.Title);
    }

    private sealed class ThrowingRule : IQueryLintRule
    {
        public string RuleId => "IDX-TEST";

        public IEnumerable<Finding> Evaluate(SearchCatalog catalog) => throw new InvalidOperationException("boom");
    }
}
