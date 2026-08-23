using Truvio.Commerce.PowerTools.Core.Search;
using Truvio.Commerce.PowerTools.Core.Search.Testing;
using Xunit;
using static Truvio.Commerce.PowerTools.Tests.SearchSpecBuilders;

namespace Truvio.Commerce.PowerTools.Tests;

public class QueryDiagnosisTests
{
    private static RunInputs Inputs(QuerySpec query, IndexSpec? index, string parameters = "", bool dropsUnknownField = false) =>
        RunInputs.For(query, index, parameters, dropsUnknownField);

    private static ClauseTrace Clause(IReadOnlyList<ClauseTrace> traces, string path) =>
        traces.Single(t => t.Path == path);

    // ---- verdicts ------------------------------------------------------------------------

    [Fact]
    public void A_parameter_without_default_or_value_drops_the_clause()
    {
        var query = Query(And(ParameterClause("Name", "q")), parameters: [Parameter("q")]);
        var traces = QueryDiagnosis.Trace(Inputs(query, Index()));

        var clause = Clause(traces, "1.1");
        Assert.Equal(ClauseVerdict.Dropped, clause.Verdict);
        Assert.Equal(ValueOrigin.MissingParameter, clause.Origin);
        Assert.Contains("MORE documents", clause.Explanation);
    }

    [Fact]
    public void A_supplied_value_makes_the_clause_active()
    {
        var query = Query(And(ParameterClause("Name", "q")), parameters: [Parameter("q")]);
        var traces = QueryDiagnosis.Trace(Inputs(query, Index(), "q=bike"));

        var clause = Clause(traces, "1.1");
        Assert.Equal(ClauseVerdict.Active, clause.Verdict);
        Assert.Equal(ValueOrigin.SuppliedValue, clause.Origin);
        Assert.Equal("bike", clause.ResolvedValue);
    }

    [Fact]
    public void A_declared_default_is_used_when_nothing_is_supplied()
    {
        var query = Query(And(ParameterClause("Name", "q")), parameters: [Parameter("q", "bike")]);
        var traces = QueryDiagnosis.Trace(Inputs(query, Index()));

        var clause = Clause(traces, "1.1");
        Assert.Equal(ClauseVerdict.Active, clause.Verdict);
        Assert.Equal(ValueOrigin.ParameterDefault, clause.Origin);
        Assert.Equal("bike", clause.ResolvedValue);
    }

    [Fact]
    public void A_blank_supplied_value_falls_back_to_the_default()
    {
        var query = Query(And(ParameterClause("Name", "q")), parameters: [Parameter("q", "bike")]);
        var traces = QueryDiagnosis.Trace(Inputs(query, Index(), "q="));

        Assert.Equal(ValueOrigin.ParameterDefault, Clause(traces, "1.1").Origin);
    }

    [Fact]
    public void An_undeclared_parameter_is_flagged_even_when_it_has_a_value()
    {
        var query = Query(And(ParameterClause("Name", "q")));
        var traces = QueryDiagnosis.Trace(Inputs(query, Index(), "q=bike"));

        var clause = Clause(traces, "1.1");
        Assert.Equal(ClauseVerdict.Active, clause.Verdict);
        Assert.Equal(ValueOrigin.UndeclaredParameter, clause.Origin);
    }

    [Fact]
    public void IsEmpty_survives_a_missing_parameter()
    {
        var query = Query(And(ParameterClause("Name", "q", "IsEmpty")), parameters: [Parameter("q")]);
        var traces = QueryDiagnosis.Trace(Inputs(query, Index()));

        var clause = Clause(traces, "1.1");
        Assert.Equal(ClauseVerdict.Active, clause.Verdict);
        Assert.Contains("IsEmpty", clause.Explanation);
    }

    [Fact]
    public void A_disabled_clause_is_reported_before_anything_else_is_evaluated()
    {
        var query = Query(And(SearchSpecBuilders.Clause("NoSuchField", disabled: true)));
        var traces = QueryDiagnosis.Trace(Inputs(query, Index()));

        Assert.Equal(ClauseVerdict.Disabled, Clause(traces, "1.1").Verdict);
    }

    [Fact]
    public void A_field_missing_from_the_schema_throws_on_the_older_platform()
    {
        // Even though the parameter has no value, the provider looks the field up first.
        var query = Query(And(ParameterClause("NoSuchField", "q")), parameters: [Parameter("q")]);
        var traces = QueryDiagnosis.Trace(Inputs(query, Index()));

        var clause = Clause(traces, "1.1");
        Assert.Equal(ClauseVerdict.Throws, clause.Verdict);
        Assert.True(QueryDiagnosis.Throws(traces));
        Assert.False(QueryDiagnosis.Collapses(traces));
    }

    [Fact]
    public void A_field_missing_from_the_schema_only_drops_the_clause_from_10_21()
    {
        var query = Query(And(ParameterClause("NoSuchField", "q")), parameters: [Parameter("q")]);
        var traces = QueryDiagnosis.Trace(Inputs(query, Index(), dropsUnknownField: true));

        var clause = Clause(traces, "1.1");
        Assert.Equal(ClauseVerdict.UnknownField, clause.Verdict);
        Assert.False(QueryDiagnosis.Throws(traces));
        // The clause is gone, so nothing constrains the query any more.
        Assert.True(QueryDiagnosis.Collapses(traces));
    }

    [Fact]
    public void An_unknown_field_is_a_fix_on_both_platforms()
    {
        var query = Query(And(ParameterClause("NoSuchField", "q")), parameters: [Parameter("q")]);

        foreach (var drops in new[] { false, true })
        {
            var inputs = Inputs(query, Index(), dropsUnknownField: drops);
            var suggestions = QueryDiagnosis.Suggest(inputs, QueryDiagnosis.Trace(inputs));

            Assert.Contains(suggestions,
                s => s.Kind == "fix" && s.Title == "Field 'NoSuchField' is not in the index schema");
        }
    }

    [Fact]
    public void A_macro_that_resolves_to_nothing_drops_the_clause()
    {
        var query = Query(And(new QueryClauseSpec("1.1", "Name", "Equal", ClauseValueKind.Macro, string.Empty, "Ctx:Missing", false)));
        var inputs = Inputs(query, Index()).WithRuntimeValues(new Dictionary<string, string> { ["1.1"] = string.Empty });
        var traces = QueryDiagnosis.Trace(inputs);

        var clause = Clause(traces, "1.1");
        Assert.Equal(ClauseVerdict.Dropped, clause.Verdict);
        Assert.Equal(ValueOrigin.Macro, clause.Origin);
    }

    [Fact]
    public void A_macro_that_resolves_stays_active_with_the_resolved_value()
    {
        var query = Query(And(new QueryClauseSpec("1.1", "Name", "Equal", ClauseValueKind.Macro, string.Empty, "Ctx:Language", false)));
        var inputs = Inputs(query, Index()).WithRuntimeValues(new Dictionary<string, string> { ["1.1"] = "LANG1" });
        var traces = QueryDiagnosis.Trace(inputs);

        var clause = Clause(traces, "1.1");
        Assert.Equal(ClauseVerdict.Active, clause.Verdict);
        Assert.Equal("LANG1", clause.ResolvedValue);
    }

    [Fact]
    public void Dropping_inside_an_Or_group_narrows_instead_of_widening()
    {
        var query = Query(
            Or(ParameterClause("Name", "q", path: "1.1"), SearchSpecBuilders.Clause("Name", path: "1.2")),
            parameters: [Parameter("q")]);
        var traces = QueryDiagnosis.Trace(Inputs(query, Index()));

        Assert.Contains("FEWER documents", Clause(traces, "1.1").Explanation);
    }

    [Fact]
    public void A_query_with_no_expression_matches_everything()
    {
        var traces = QueryDiagnosis.Trace(Inputs(Query(null), Index()));

        Assert.Equal(ClauseVerdict.MatchesEverything, traces.Single().Verdict);
        Assert.True(QueryDiagnosis.Collapses(traces));
    }

    [Fact]
    public void A_tree_where_every_clause_drops_collapses_to_match_all()
    {
        var query = Query(
            And(ParameterClause("Name", "a", path: "1.1"), ParameterClause("Name", "b", path: "1.2")),
            parameters: [Parameter("a"), Parameter("b")]);
        var traces = QueryDiagnosis.Trace(Inputs(query, Index()));

        Assert.True(QueryDiagnosis.Collapses(traces));
    }

    [Fact]
    public void Blank_free_text_drops_and_real_free_text_stays()
    {
        var blank = QueryDiagnosis.Trace(Inputs(Query(And(new QueryFullTextSpec("1.1", ["Name"], "  "))), Index()));
        Assert.Equal(ClauseVerdict.Dropped, Clause(blank, "1.1").Verdict);

        var text = QueryDiagnosis.Trace(Inputs(Query(And(new QueryFullTextSpec("1.1", ["Name"], "bike"))), Index()));
        Assert.Equal(ClauseVerdict.Active, Clause(text, "1.1").Verdict);
    }

    [Fact]
    public void Group_rows_carry_their_operator_and_child_count()
    {
        var query = Query(And(SearchSpecBuilders.Clause("Name", path: "1.1")));
        var traces = QueryDiagnosis.Trace(Inputs(query, Index()));

        var group = Clause(traces, "1");
        Assert.True(group.IsGroup);
        Assert.Equal("And", group.Operator);
        Assert.Contains("All of (1)", group.Label);
    }

    // ---- suggestions ---------------------------------------------------------------------

    [Fact]
    public void The_blank_parameter_leak_produces_a_fix_suggestion()
    {
        var query = Query(And(ParameterClause("Name", "q")), parameters: [Parameter("q")]);
        var inputs = Inputs(query, Index());

        var suggestions = QueryDiagnosis.Suggest(inputs, QueryDiagnosis.Trace(inputs));

        Assert.Contains(suggestions, s => s.Kind == "fix" && s.Title.Contains("'q' has no value"));
        Assert.Contains(suggestions, s => s.Title.Contains("EVERY document"));
    }

    [Fact]
    public void A_stored_but_unindexed_clause_field_is_reported_as_never_matching()
    {
        var index = Index([Field("Name", indexed: false)]);
        var query = Query(And(SearchSpecBuilders.Clause("Name", value: "bike")));
        var inputs = Inputs(query, index);

        var suggestions = QueryDiagnosis.Suggest(inputs, QueryDiagnosis.Trace(inputs));

        Assert.Contains(suggestions, s => s.Kind == "fix" && s.Title.Contains("stored but not indexed"));
    }

    [Fact]
    public void Equal_against_an_analyzed_field_warns_about_the_analyzer()
    {
        var index = Index([Field("Name", analyzed: true)]);
        var query = Query(And(SearchSpecBuilders.Clause("Name", value: "Mountain Bike")));
        var inputs = Inputs(query, index);

        var suggestions = QueryDiagnosis.Suggest(inputs, QueryDiagnosis.Trace(inputs));

        Assert.Contains(suggestions, s => s.Kind == "warn" && s.Title.Contains("analyzed field 'Name'"));
    }

    [Fact]
    public void Contains_against_an_analyzed_field_does_not_warn()
    {
        var index = Index([Field("Name", analyzed: true)]);
        var query = Query(And(SearchSpecBuilders.Clause("Name", op: "Contains", value: "bike")));
        var inputs = Inputs(query, index);

        var suggestions = QueryDiagnosis.Suggest(inputs, QueryDiagnosis.Trace(inputs));

        Assert.DoesNotContain(suggestions, s => s.Title.Contains("analyzed field"));
    }

    [Fact]
    public void A_missing_source_index_is_a_fix()
    {
        var query = Query(And(SearchSpecBuilders.Clause("Name")), sourceItem: "Gone.index");
        var inputs = Inputs(query, null);

        var suggestions = QueryDiagnosis.Suggest(inputs, QueryDiagnosis.Trace(inputs));

        Assert.Contains(suggestions, s => s.Kind == "fix" && s.Title.Contains("source index does not exist"));
    }

    [Fact]
    public void A_never_built_index_is_a_fix_and_a_stale_one_only_a_warning()
    {
        var query = Query(And(SearchSpecBuilders.Clause("Name")));

        var never = QueryDiagnosis.Suggest(
            Inputs(query, Index(health: IndexHealth.NeverBuilt)),
            QueryDiagnosis.Trace(Inputs(query, Index(health: IndexHealth.NeverBuilt))));
        Assert.Contains(never, s => s.Kind == "fix" && s.Title.Contains("never been built"));

        var stale = QueryDiagnosis.Suggest(
            Inputs(query, Index(health: IndexHealth.Stale)),
            QueryDiagnosis.Trace(Inputs(query, Index(health: IndexHealth.Stale))));
        Assert.Contains(stale, s => s.Kind == "warn" && s.Title.Contains("stale"));
    }

    [Fact]
    public void A_value_supplied_for_something_the_query_never_reads_is_only_a_note()
    {
        var query = Query(And(SearchSpecBuilders.Clause("Name")));
        var inputs = Inputs(query, Index(), "unknown=x");

        var suggestions = QueryDiagnosis.Suggest(inputs, QueryDiagnosis.Trace(inputs));

        Assert.Contains(suggestions, s => s.Kind == "info" && s.Title.Contains("'unknown' is not a parameter"));
    }

    [Fact]
    public void Suggestions_are_ordered_fix_then_warn_then_note()
    {
        var index = Index([Field("Name", analyzed: true)], health: IndexHealth.Stale);
        var query = Query(
            And(SearchSpecBuilders.Clause("Name", value: "Mountain Bike", path: "1.1"),
                SearchSpecBuilders.Clause("Missing", path: "1.2")));
        var inputs = Inputs(query, index, "unknown=x");

        var suggestions = QueryDiagnosis.Suggest(inputs, QueryDiagnosis.Trace(inputs));

        Assert.Equal(suggestions.OrderBy(s => s.Rank).Select(s => s.Title), suggestions.Select(s => s.Title));
        Assert.Equal("fix", suggestions[0].Kind);
    }

    // ---- measured suggestions ------------------------------------------------------------

    [Fact]
    public void A_clause_that_matches_nothing_on_its_own_is_called_out()
    {
        var impacts = new[] { new ClauseImpact("1.1", "Color Equal blue", 40, 0) };

        var suggestions = QueryDiagnosis.SuggestFromImpact(0, 40, impacts);

        Assert.Contains(suggestions, s => s.Kind == "fix" && s.Title.Contains("matches no document at all"));
    }

    [Fact]
    public void An_empty_result_names_the_clause_that_causes_it()
    {
        var impacts = new[] { new ClauseImpact("1.2", "Active Equal false", 120, 5) };

        var suggestions = QueryDiagnosis.SuggestFromImpact(0, 120, impacts);

        Assert.Contains(suggestions, s => s.Kind == "fix" && s.Title.Contains("1.2 is why this run returns nothing"));
    }

    [Fact]
    public void A_clause_that_removes_nothing_is_only_a_note()
    {
        var impacts = new[] { new ClauseImpact("1.1", "LanguageID Equal LANG1", 12, 12) };

        var suggestions = QueryDiagnosis.SuggestFromImpact(12, 12, impacts);

        Assert.Contains(suggestions, s => s.Kind == "info" && s.Title.Contains("changes nothing"));
    }

    // ---- why not X -----------------------------------------------------------------------

    [Fact]
    public void A_document_that_is_not_in_the_index_short_circuits_the_explanation()
    {
        var suggestions = QueryDiagnosis.SuggestFromExpectation(
            [], "PROD27", foundInIndex: false, [], new Dictionary<string, string>());

        Assert.Single(suggestions);
        Assert.Contains("not in the index at all", suggestions[0].Title);
    }

    [Fact]
    public void A_failing_clause_reports_the_documents_own_value()
    {
        var query = Query(And(SearchSpecBuilders.Clause("Color", value: "blue")));
        var traces = QueryDiagnosis.Trace(Inputs(query, Index([Field("Color")])));
        var checks = new[] { new ExpectationCheck("1.1", "Color Equal blue", "Color", "red", false, "Expected Equal 'blue'.") };

        var suggestions = QueryDiagnosis.SuggestFromExpectation(
            traces, "PROD27", true, checks,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Color"] = "red" });

        Assert.Contains(suggestions, s => s.Kind == "fix" && s.Detail.Contains("Color = 'red'"));
    }

    [Fact]
    public void A_value_found_in_a_sibling_field_is_reported_as_the_wrong_field()
    {
        var query = Query(And(SearchSpecBuilders.Clause("Color", value: "blue")));
        var traces = QueryDiagnosis.Trace(Inputs(query, Index([Field("Color")])));
        var checks = new[] { new ExpectationCheck("1.1", "Color Equal blue", "Color", "red", false, string.Empty) };

        var suggestions = QueryDiagnosis.SuggestFromExpectation(
            traces, "PROD27", true, checks,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Color"] = "red",
                ["ColourFacet"] = "blue"
            });

        Assert.Contains(suggestions, s => s.Kind == "warn" && s.Title.Contains("lives in 'ColourFacet'"));
    }

    [Fact]
    public void A_document_that_passes_everything_points_at_paging_and_sort()
    {
        var query = Query(And(SearchSpecBuilders.Clause("Color", value: "blue")));
        var traces = QueryDiagnosis.Trace(Inputs(query, Index([Field("Color")])));
        var checks = new[] { new ExpectationCheck("1.1", "Color Equal blue", "Color", "blue", true, string.Empty) };

        var suggestions = QueryDiagnosis.SuggestFromExpectation(
            traces, "PROD27", true, checks,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Color"] = "blue" });

        Assert.Contains(suggestions, s => s.Kind == "info" && s.Title.Contains("passes every active clause"));
    }
}
