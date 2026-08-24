using Truvio.Commerce.PowerTools.Core.Permissions;
using Truvio.Commerce.PowerTools.Core.Principals;
using Xunit;

namespace Truvio.Commerce.PowerTools.Tests;

public class AccessExplanationTests
{
    private static readonly Func<string?, string> Names = ownerId => ownerId switch
    {
        null => "none",
        SecurityAccount.AuthenticatedFrontendRole => "Authenticated frontend role",
        SecurityAccount.AnonymousRole => "Anonymous role",
        "60" => "CSR Team",
        "55" => "Jacob's Lumber Co.",
        _ => ownerId
    };

    private static SecurityAccount Jacobs() => new()
    {
        Kind = SecurityAccountKind.Group,
        Id = "55",
        DisplayName = "Jacob's Lumber Co.",
        OwnerIds = [SecurityAccount.AuthenticatedFrontendRole, "55"]
    };

    private static ContentPermissionRow Row(string owner, int level) =>
        new(owner, ContentEntityNames.Page, "10", level);

    // The gate that motivated all of this: broad role -> None plus a grant for one group.
    [Fact]
    public void GatedPage_SaysWhyMembershipDoesNotHelpAndWhoGetsIn()
    {
        var access = new EffectiveAccess(Levels.None, AccessOrigin.ExplicitHere, 10, SecurityAccount.AuthenticatedFrontendRole);
        var rows = new[] { Row(SecurityAccount.AuthenticatedFrontendRole, Levels.None), Row("60", Levels.Read) };

        var text = AccessExplanation.Explain(Jacobs(), access, rows, null, Names);

        Assert.Contains("Gated here", text);
        Assert.Contains("'Authenticated frontend role' is set to None", text);
        Assert.Contains("'Jacob's Lumber Co.' has no grant of its own", text);
        Assert.Contains("Only 'CSR Team' can see it", text);
    }

    [Fact]
    public void OwnExplicitNone_IsCalledOutDirectly()
    {
        var access = new EffectiveAccess(Levels.None, AccessOrigin.ExplicitHere, 10, "55");
        var rows = new[] { Row("55", Levels.None), Row("60", Levels.Read) };

        var text = AccessExplanation.Explain(Jacobs(), access, rows, null, Names);

        Assert.Contains("'Jacob's Lumber Co.' is explicitly set to None here", text);
        Assert.Contains("Only 'CSR Team' can see it", text);
    }

    [Fact]
    public void Grant_NamesTheWinningRowAndLevel()
    {
        var access = new EffectiveAccess(Levels.Read, AccessOrigin.ExplicitHere, 10, "55");
        var rows = new[] { Row("55", Levels.Read) };

        var text = AccessExplanation.Explain(Jacobs(), access, rows, null, Names);

        Assert.Equal("Set here: 'Jacob's Lumber Co.' grants Read.", text);
    }

    [Fact]
    public void InheritedGate_NamesTheAncestorPage()
    {
        var access = new EffectiveAccess(Levels.None, AccessOrigin.InheritedFromPage, 5, SecurityAccount.AuthenticatedFrontendRole);
        var rows = new[] { Row(SecurityAccount.AuthenticatedFrontendRole, Levels.None), Row("60", Levels.Read) };

        var text = AccessExplanation.Explain(Jacobs(), access, rows, "Customer center", Names);

        Assert.Contains("Gated on 'Customer center'", text);
    }

    [Fact]
    public void GateWithNoGrants_SaysOnlyAdministratorsSeeIt()
    {
        var access = new EffectiveAccess(Levels.None, AccessOrigin.ExplicitHere, 10, SecurityAccount.AuthenticatedFrontendRole);
        var rows = new[] { Row(SecurityAccount.AuthenticatedFrontendRole, Levels.None) };

        var text = AccessExplanation.Explain(Jacobs(), access, rows, null, Names);

        Assert.Contains("Nothing is granted - only administrators see this.", text);
    }

    [Fact]
    public void LongGrantList_IsCapped()
    {
        var access = new EffectiveAccess(Levels.None, AccessOrigin.ExplicitHere, 10, SecurityAccount.AuthenticatedFrontendRole);
        var rows = new List<ContentPermissionRow> { Row(SecurityAccount.AuthenticatedFrontendRole, Levels.None) };
        for (var i = 0; i < 6; i++)
            rows.Add(Row($"7{i}", Levels.Read));

        var text = AccessExplanation.Explain(Jacobs(), access, rows, null, Names);

        Assert.Contains("and 2 more", text);
    }

    [Fact]
    public void RoleDefault_StaysConcise()
    {
        var access = new EffectiveAccess(Levels.Read, AccessOrigin.RoleDefault, null, SecurityAccount.AuthenticatedFrontendRole);

        var text = AccessExplanation.Explain(Jacobs(), access, [], null, Names);

        Assert.Equal("Role default (Authenticated frontend role)", text);
    }
}
