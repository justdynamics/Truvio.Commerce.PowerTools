using Truvio.Commerce.PowerTools.Core.Diagnostics;
using Truvio.Commerce.PowerTools.Core.Search;
using Truvio.Commerce.PowerTools.Core.Search.Rules;
using Truvio.Commerce.PowerTools.Core.Settings;
using Xunit;
using static Truvio.Commerce.PowerTools.Tests.SearchSpecBuilders;

namespace Truvio.Commerce.PowerTools.Tests;

public class PowerToolsSettingsTests
{
    private static Finding Lint(string ruleId, string entityKey = "Products/Products.query",
        string display = "Products (Products)", string? subject = null) =>
        new(ruleId, FindingSeverity.Warning, SearchEntityNames.Query, entityKey, display, "title", "detail", subject);

    // ---- list parsing ------------------------------------------------------------------

    [Theory]
    [InlineData("IDX-W1,IDX-W2")]
    [InlineData("IDX-W1\nIDX-W2")]
    [InlineData("IDX-W1\r\nIDX-W2")]
    [InlineData(" IDX-W1 ; IDX-W2 ")]
    [InlineData("IDX-W1 IDX-W2")]
    public void SplitList_accepts_every_separator_a_config_store_can_produce(string raw)
    {
        Assert.Equal(["IDX-W1", "IDX-W2"], PowerToolsSettings.SplitList(raw));
    }

    [Fact]
    public void SplitList_is_empty_for_blank_input()
    {
        Assert.Empty(PowerToolsSettings.SplitList(null));
        Assert.Empty(PowerToolsSettings.SplitList("   "));
    }

    [Fact]
    public void SplitList_drops_duplicates_case_insensitively()
    {
        Assert.Equal(["idx-w1"], PowerToolsSettings.SplitList("idx-w1, IDX-W1"));
    }

    // ---- matching -----------------------------------------------------------------------

    [Fact]
    public void IsRuleIgnored_matches_exactly_and_case_insensitively()
    {
        var settings = new PowerToolsSettings { IgnoredRules = "IDX-W5" };

        Assert.True(settings.IsRuleIgnored("idx-w5"));
        Assert.False(settings.IsRuleIgnored("IDX-W50"));
        Assert.False(settings.IsRuleIgnored("IDX-W1"));
    }

    [Fact]
    public void A_trailing_star_ignores_a_whole_rule_family()
    {
        var settings = new PowerToolsSettings { SuppressedWarningRules = "SECOPS-W*" };

        Assert.True(settings.IsWarningRuleSuppressed("SECOPS-W1"));
        Assert.True(settings.IsWarningRuleSuppressed("SECOPS-W4"));
        Assert.False(settings.IsWarningRuleSuppressed("IDX-W1"));
    }

    [Fact]
    public void IsQueryIgnored_matches_the_bare_name_the_key_or_the_display_form()
    {
        Assert.True(new PowerToolsSettings { IgnoredQueries = "Products" }
            .IsQueryIgnored("Products/Products.query", "Products (Products)"));
        Assert.True(new PowerToolsSettings { IgnoredQueries = "Products/Products.query" }
            .IsQueryIgnored("Products/Products.query", "Products (Products)"));
        Assert.False(new PowerToolsSettings { IgnoredQueries = "Users" }
            .IsQueryIgnored("Products/Products.query", "Products (Products)"));
    }

    // ---- suppression ----------------------------------------------------------------------

    [Fact]
    public void Default_settings_hide_nothing()
    {
        var result = PowerToolsSettings.Defaults.FilterSearchFindings([Lint("IDX-W1", subject: "eq")]);

        Assert.Single(result.Visible);
        Assert.Equal(0, result.HiddenCount);
    }

    [Fact]
    public void An_ignored_rule_is_dropped_and_counted()
    {
        var settings = new PowerToolsSettings { IgnoredRules = "IDX-W1" };

        var result = settings.FilterSearchFindings([Lint("IDX-W1"), Lint("IDX-W2")]);

        Assert.Single(result.Visible);
        Assert.Equal(1, result.HiddenCount);
        Assert.Equal("1 finding hidden by settings", result.HiddenNotice());
    }

    [Fact]
    public void An_ignored_parameter_only_drops_findings_about_that_parameter()
    {
        var settings = new PowerToolsSettings { IgnoredParameters = "eq" };

        var result = settings.FilterSearchFindings(
        [
            Lint("IDX-W1", subject: "eq"),
            Lint("IDX-W1", subject: "active"),
            Lint("IDX-W1")
        ]);

        Assert.Equal(2, result.Visible.Count);
        Assert.Equal(1, result.HiddenCount);
    }

    [Fact]
    public void A_collapsing_query_is_hidden_only_when_every_parameter_behind_it_is_ignored()
    {
        var settings = new PowerToolsSettings { IgnoredParameters = "eq, q" };

        Assert.Equal(0, settings.FilterSearchFindings([Lint("IDX-W2", subject: "eq,q")]).Visible.Count);
        Assert.Single(settings.FilterSearchFindings([Lint("IDX-W2", subject: "eq,active")]).Visible);
    }

    [Fact]
    public void Warning_suppression_uses_its_own_list()
    {
        var settings = new PowerToolsSettings { IgnoredRules = "SECOPS-W1" };

        // The linter list must not silence the content warnings, and vice versa.
        Assert.Single(settings.FilterWarningFindings([Lint("SECOPS-W1")]).Visible);
        Assert.Empty(new PowerToolsSettings { SuppressedWarningRules = "SECOPS-W1" }
            .FilterWarningFindings([Lint("SECOPS-W1")]).Visible);
    }

    [Fact]
    public void HiddenNotice_pluralises()
    {
        Assert.Equal("3 findings hidden by settings", new FindingFilter([], 3).HiddenNotice());
        Assert.Equal("1 finding hidden by settings", new FindingFilter([], 1).HiddenNotice());
    }

    // ---- presets -----------------------------------------------------------------------------

    [Fact]
    public void Quantity_presets_parse_and_drop_nonsense()
    {
        var settings = new PowerToolsSettings { QuantityPresets = "1, 2.5, oops, -3, 10" };

        Assert.Equal([1, 2.5, 10], settings.Quantities());
    }

    [Fact]
    public void Empty_presets_fall_back_to_the_shipped_list()
    {
        Assert.Equal([1, 5, 10, 25, 50, 100, 500], new PowerToolsSettings { QuantityPresets = "" }.Quantities());
        Assert.Equal([7, 30, 90], new PowerToolsSettings { DatePresetDays = "   " }.DateOffsets());
    }

    [Fact]
    public void Date_presets_parse_to_whole_days()
    {
        Assert.Equal([1, 14], new PowerToolsSettings { DatePresetDays = "1,14" }.DateOffsets());
    }

    [Fact]
    public void Positive_rejects_a_zero_or_negative_stored_value()
    {
        Assert.Equal(200, PowerToolsSettings.Positive(0, 200));
        Assert.Equal(200, PowerToolsSettings.Positive(-5, 200));
        Assert.Equal(50, PowerToolsSettings.Positive(50, 200));
    }

    // ---- the rules that feed the suppression -------------------------------------------------

    [Fact]
    public void IDX_W1_names_the_parameter_it_is_about()
    {
        var query = Query(And(Clause("Name"), ParameterClause("Active", "active")), [Parameter("active")]);

        var finding = Assert.Single(new BlankParameterClauseRule().Evaluate(Catalog(queries: [query])));

        Assert.Equal("active", finding.Subject);
    }

    [Fact]
    public void IDX_W2_names_every_parameter_that_collapses_the_query()
    {
        var query = Query(
            And(ParameterClause("Active", "active"), ParameterClause("Name", "q")),
            [Parameter("active"), Parameter("q")]);

        var finding = Assert.Single(new QueryMatchesEverythingRule().Evaluate(Catalog(queries: [query])));

        Assert.Equal("active,q", finding.Subject);

        // ... and the whole finding disappears once both parameters are muted.
        Assert.Empty(new PowerToolsSettings { IgnoredParameters = "active, q" }
            .FilterSearchFindings([finding]).Visible);
    }
}
