using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.IndexInspector.SearchSpecBuilders;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Core;

namespace Truvio.Commerce.PowerTools.Tests.Features.IndexInspector;

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
