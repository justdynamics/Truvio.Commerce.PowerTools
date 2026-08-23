using Truvio.Commerce.PowerTools.Core.Search.Testing;
using Xunit;
using static Truvio.Commerce.PowerTools.Tests.SearchSpecBuilders;

namespace Truvio.Commerce.PowerTools.Tests;

public class ParameterValuesTests
{
    [Fact]
    public void Parse_reads_name_value_pairs()
    {
        var values = ParameterValues.Parse("eq=blue;GroupID=SHOP1");

        Assert.Equal("blue", values["eq"]);
        Assert.Equal("SHOP1", values["GroupID"]);
    }

    [Fact]
    public void Parse_keeps_equals_signs_inside_the_value()
    {
        var values = ParameterValues.Parse("filter=a=b");

        Assert.Equal("a=b", values["filter"]);
    }

    [Fact]
    public void Parse_accepts_names_with_spaces()
    {
        var values = ParameterValues.Parse("Bike type=Gravel");

        Assert.Equal("Gravel", values["Bike type"]);
    }

    [Fact]
    public void Parse_is_case_insensitive_and_last_assignment_wins()
    {
        var values = ParameterValues.Parse("eq=blue;EQ=red");

        Assert.Single(values);
        Assert.Equal("red", values["eq"]);
    }

    [Fact]
    public void Effective_drops_blank_values_so_the_clause_disappears_rather_than_matching_empty()
    {
        var values = ParameterValues.Effective("eq=;GroupID=SHOP1");

        Assert.False(values.ContainsKey("eq"));
        Assert.Equal("SHOP1", values["GroupID"]);
    }

    [Fact]
    public void Effective_hides_the_testers_own_reserved_settings()
    {
        var values = ParameterValues.Effective("#expect=PROD27;eq=blue");

        Assert.False(values.ContainsKey(ParameterValues.ExpectKeyName));
        Assert.Equal("blue", values["eq"]);
        Assert.Equal("PROD27", ParameterValues.Reserved("#expect=PROD27;eq=blue", ParameterValues.ExpectKeyName));
    }

    [Fact]
    public void Set_replaces_in_place_and_null_removes()
    {
        var text = ParameterValues.Set("eq=blue;GroupID=SHOP1", "eq", "red");
        Assert.Equal("eq=red;GroupID=SHOP1", text);

        Assert.Equal("GroupID=SHOP1", ParameterValues.Set(text, "eq", null));
    }

    [Fact]
    public void Merge_applies_a_typed_assignment_and_ignores_plain_text()
    {
        Assert.Equal("eq=blue;GroupID=SHOP2",
            ParameterValues.Merge("eq=blue;GroupID=SHOP1", "GroupID=SHOP2"));

        Assert.Equal("eq=blue", ParameterValues.Merge("eq=blue", "just a search term"));
    }

    [Fact]
    public void Defaults_lists_only_parameters_that_carry_one()
    {
        var query = Query(And(ParameterClause("Name", "q")),
            parameters: [Parameter("q"), Parameter("LanguageID", "LANG1")]);

        Assert.Equal("LanguageID=LANG1", ParameterValues.Defaults(query));
    }
}
