using Truvio.Commerce.PowerTools.Core.Commerce;
using Xunit;

namespace Truvio.Commerce.PowerTools.Tests;

public class AssortmentEvaluatorTests
{
    private static AssortmentSpec Assortment(
        string id,
        bool contains = true,
        bool active = true,
        bool anonymous = false,
        int[]? users = null,
        int[]? groups = null) => new()
    {
        Id = id,
        Name = id,
        Active = active,
        AllowAnonymousUsers = anonymous,
        ContainsProduct = contains,
        PermittedUserIds = (users ?? []).ToHashSet(),
        PermittedGroupIds = (groups ?? []).ToHashSet()
    };

    private static readonly AssortmentAccount Anonymous = new();

    private static AssortmentAccount User(int id, params int[] groups) => new() { UserId = id, GroupIds = groups.ToHashSet() };

    [Fact]
    public void Assortments_disabled_means_everything_is_visible()
    {
        var verdict = AssortmentEvaluator.Evaluate([Assortment("A", groups: [1])], User(9), useAssortmentsSetting: false);

        Assert.Equal(VisibilityOutcome.AssortmentsInactive, verdict.Outcome);
        Assert.True(verdict.Visible);
    }

    [Fact]
    public void No_active_assortment_means_everything_is_visible()
    {
        var verdict = AssortmentEvaluator.Evaluate([Assortment("A", active: false)], User(9), useAssortmentsSetting: true);

        Assert.Equal(VisibilityOutcome.AssortmentsInactive, verdict.Outcome);
    }

    [Fact]
    public void Product_in_no_assortment_is_visible_with_list_warning()
    {
        var verdict = AssortmentEvaluator.Evaluate([Assortment("A", contains: false, groups: [1])], User(9), true);

        Assert.Equal(VisibilityOutcome.InNoAssortment, verdict.Outcome);
        Assert.True(verdict.Visible);
        Assert.Contains(verdict.Warnings, w => w.Contains("assortment-filtered"));
    }

    [Fact]
    public void Group_membership_grants_the_assortment()
    {
        var verdict = AssortmentEvaluator.Evaluate([Assortment("A", groups: [1])], User(9, 1), true);

        Assert.Equal(VisibilityOutcome.Visible, verdict.Outcome);
        Assert.True(verdict.Rows.Single().Grants);
        Assert.Contains("group 1", verdict.Rows.Single().Explanation);
    }

    [Fact]
    public void Direct_user_permission_grants_the_assortment()
    {
        var verdict = AssortmentEvaluator.Evaluate([Assortment("A", users: [9])], User(9), true);

        Assert.Equal(VisibilityOutcome.Visible, verdict.Outcome);
    }

    [Fact]
    public void Non_member_is_hidden_with_explanation()
    {
        var verdict = AssortmentEvaluator.Evaluate([Assortment("A", groups: [1])], User(9, 2), true);

        Assert.Equal(VisibilityOutcome.Hidden, verdict.Outcome);
        Assert.False(verdict.Visible);
        Assert.Contains("not among", verdict.Rows.Single().Explanation);
    }

    [Fact]
    public void Anonymous_needs_the_anonymous_flag()
    {
        Assert.Equal(VisibilityOutcome.Hidden,
            AssortmentEvaluator.Evaluate([Assortment("A", groups: [1])], Anonymous, true).Outcome);
        Assert.Equal(VisibilityOutcome.Visible,
            AssortmentEvaluator.Evaluate([Assortment("A", anonymous: true)], Anonymous, true).Outcome);
    }

    [Fact]
    public void Inactive_assortment_still_restricts_but_never_grants()
    {
        // DW collects containing assortments from ALL assortments, then grants only through ACTIVE ones.
        var specs = new[]
        {
            Assortment("inactive", active: false, groups: [1]),
            Assortment("other", contains: false)
        };

        var verdict = AssortmentEvaluator.Evaluate(specs, User(9, 1), true);

        Assert.Equal(VisibilityOutcome.Hidden, verdict.Outcome);
        Assert.Contains(verdict.Warnings, w => w.Contains("INACTIVE"));
    }

    [Fact]
    public void One_granting_assortment_is_enough_even_if_others_deny()
    {
        var specs = new[]
        {
            Assortment("denied", groups: [5]),
            Assortment("granted", groups: [1])
        };

        var verdict = AssortmentEvaluator.Evaluate(specs, User(9, 1), true);

        Assert.Equal(VisibilityOutcome.Visible, verdict.Outcome);
        Assert.Single(verdict.Rows, r => r.Grants);
    }

    [Fact]
    public void Rebuild_required_on_a_containing_assortment_warns()
    {
        var spec = Assortment("A", groups: [1]) with { RebuildRequired = true };

        var verdict = AssortmentEvaluator.Evaluate([spec], User(9, 1), true);

        Assert.Contains(verdict.Warnings, w => w.Contains("rebuild"));
    }
}
