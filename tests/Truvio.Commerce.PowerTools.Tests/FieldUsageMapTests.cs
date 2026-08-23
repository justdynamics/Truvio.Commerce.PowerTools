using Truvio.Commerce.PowerTools.Core.Search;
using Xunit;
using static Truvio.Commerce.PowerTools.Tests.SearchSpecBuilders;

namespace Truvio.Commerce.PowerTools.Tests;

public class FieldUsageMapTests
{
    [Fact]
    public void A_field_used_by_a_clause_a_sort_and_a_facet_lists_all_three()
    {
        var index = Index([Field("Name"), Field("Active", "System.Boolean")]);
        var query = Query(
            And(Clause("Name")),
            [Parameter("brand")],
            [new QuerySortSpec("Name", "Ascending")]);
        var facets = Facets([Facet("Brand", "Name", "brand")]);

        var usage = FieldUsageMap.Build(Catalog([index], [query], [facets]))
            .Single(u => u.FieldName == "Name");

        Assert.Equal(1, usage.ExpressionCount);
        Assert.Equal(1, usage.SortCount);
        Assert.Equal(1, usage.FacetCount);
        Assert.Equal("Used", usage.Status);
        Assert.Equal("1 clause, 1 sort, 1 facet", usage.UsageSummary());
    }

    [Fact]
    public void An_indexed_field_nothing_references_is_unused()
    {
        var index = Index([Field("Name"), Field("Lonely")]);
        var query = Query(And(Clause("Name")));

        var usage = FieldUsageMap.Build(Catalog([index], [query])).Single(u => u.FieldName == "Lonely");

        Assert.True(usage.Dead);
        Assert.False(usage.Dangling);
        Assert.Equal("Unused", usage.Status);
        Assert.Equal("-", usage.UsageSummary());
    }

    [Fact]
    public void A_stored_only_field_nothing_references_is_not_reported_as_unused()
    {
        var index = Index([Field("Name"), Field("Blob", indexed: false)]);
        var query = Query(And(Clause("Name")));

        var usage = FieldUsageMap.Build(Catalog([index], [query])).Single(u => u.FieldName == "Blob");

        Assert.False(usage.Dead);
        Assert.Equal("Stored only", usage.Status);
    }

    [Fact]
    public void A_reference_to_a_field_outside_the_schema_becomes_a_dangling_row()
    {
        var index = Index([Field("Name")]);
        var query = Query(And(Clause("Name"), Clause("Ghost")));

        var rows = FieldUsageMap.Build(Catalog([index], [query]));
        var dangling = rows.Single(r => r.FieldName == "Ghost");

        Assert.True(dangling.Dangling);
        Assert.Null(dangling.Field);
        Assert.Equal("Dangling", dangling.Status);
        Assert.Equal(1, dangling.ExpressionCount);
    }

    [Fact]
    public void Sorting_on_score_is_not_treated_as_a_field_reference()
    {
        var index = Index([Field("Name")]);
        var query = Query(And(Clause("Name")), sortOrder: [new QuerySortSpec("_score", "Descending")]);

        var rows = FieldUsageMap.Build(Catalog([index], [query]));

        Assert.DoesNotContain(rows, r => r.FieldName == "_score");
    }

    [Fact]
    public void Facets_reach_the_index_through_their_source_query()
    {
        var index = Index([Field("Name")]);
        var query = Query(And(Clause("Name")), [Parameter("brand")]);
        var facets = Facets([Facet("Brand", "Name", "brand")]);

        var usage = FieldUsageMap.Build(Catalog([index], [query], [facets])).Single(u => u.FieldName == "Name");

        Assert.Equal(1, usage.FacetCount);
        Assert.Contains(usage.References, r => r.Kind == FieldUsageKind.Facet && r.Owner == "Products");
    }

    [Fact]
    public void A_disabled_clause_still_counts_as_a_reference_but_says_so()
    {
        var index = Index([Field("Name")]);
        var query = Query(And(Clause("Name", disabled: true)));

        var usage = FieldUsageMap.Build(Catalog([index], [query])).Single(u => u.FieldName == "Name");

        Assert.Equal(1, usage.ExpressionCount);
        Assert.Contains(usage.References, r => r.Detail.Contains("disabled"));
    }
}

public class LuceneSemanticsTests
{
    [Fact]
    public void A_nested_group_that_empties_out_collapses_its_parent_too()
    {
        var query = Query(
            And(Or(ParameterClause("Name", "a", path: "1.1.1"), ParameterClause("Name", "b", path: "1.1.2"))),
            [Parameter("a"), Parameter("b")]);

        Assert.True(LuceneSemantics.Collapses(query));
    }

    [Fact]
    public void A_surviving_clause_anywhere_keeps_the_query_alive()
    {
        var query = Query(
            And(Or(ParameterClause("Name", "a", path: "1.1.1"), Clause("Active", path: "1.1.2"))),
            [Parameter("a")]);

        Assert.False(LuceneSemantics.Collapses(query));
    }

    [Fact]
    public void Clauses_are_paired_with_the_group_that_holds_them()
    {
        var inner = Or(Clause("Name", path: "1.1.1"));
        var query = Query(And(inner, Clause("Active", path: "1.2")));

        var pairs = LuceneSemantics.ClausesWithParent(query).ToList();

        Assert.Equal(2, pairs.Count);
        Assert.False(pairs[0].Parent!.IsAnd);
        Assert.True(pairs[1].Parent!.IsAnd);
    }
}
