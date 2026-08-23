using Truvio.Commerce.PowerApps.SecOps.Core.Permissions;
using Truvio.Commerce.PowerApps.SecOps.Core.Principals;
using Xunit;

namespace Truvio.Commerce.PowerApps.SecOps.Tests;

public class EffectiveAccessEvaluatorTests
{
    private static SecurityAccount Role(string role) => new()
    {
        Kind = SecurityAccountKind.Role,
        Id = role,
        DisplayName = role,
        OwnerIds = [role]
    };

    private static SecurityAccount Group(string groupId) => new()
    {
        Kind = SecurityAccountKind.Group,
        Id = groupId,
        DisplayName = $"Group {groupId}",
        OwnerIds = [SecurityAccount.AuthenticatedFrontendRole, groupId]
    };

    private static SecurityAccount UserIn(params string[] groupIds) => new()
    {
        Kind = SecurityAccountKind.User,
        Id = "17",
        DisplayName = "user",
        OwnerIds = [SecurityAccount.AuthenticatedFrontendRole, .. groupIds]
    };

    private static SecurityAccount Admin() => new()
    {
        Kind = SecurityAccountKind.User,
        Id = "1",
        DisplayName = "admin",
        OwnerIds = [SecurityAccount.AuthenticatedFrontendRole],
        BypassesChecks = true
    };

    private static FakeContentSecuritySource SourceWithPages(params PageNode[] pages)
    {
        var source = new FakeContentSecuritySource();
        source.Areas.Add(new AreaNode(1, "Site"));
        source.Pages.AddRange(pages);
        return source;
    }

    private static PageNode P(int id, int parentId = 0) => new(id, parentId, 1, $"Page{id}", id, true, false);

    private static Dictionary<int, PageNode> Index(FakeContentSecuritySource source) =>
        source.Pages.ToDictionary(p => p.Id);

    [Fact]
    public void UngatedPage_FrontendRolesGetDefaultRead()
    {
        var source = SourceWithPages(P(10));
        var evaluator = new EffectiveAccessEvaluator(source);

        var anon = evaluator.EvaluatePage(Role(SecurityAccount.AnonymousRole), 10, Index(source));
        var auth = evaluator.EvaluatePage(Role(SecurityAccount.AuthenticatedFrontendRole), 10, Index(source));

        Assert.True(anon.GrantsRead);
        Assert.Equal(AccessOrigin.RoleDefault, anon.Origin);
        Assert.True(auth.GrantsRead);
    }

    [Fact]
    public void DenyGrantPair_GatesToGroupOnly()
    {
        var source = SourceWithPages(P(10));
        source.Rows.Add(new ContentPermissionRow(SecurityAccount.AuthenticatedFrontendRole, ContentEntityNames.Page, "10", Levels.None));
        source.Rows.Add(new ContentPermissionRow(SecurityAccount.AnonymousRole, ContentEntityNames.Page, "10", Levels.None));
        source.Rows.Add(new ContentPermissionRow("42", ContentEntityNames.Page, "10", Levels.Read));
        var evaluator = new EffectiveAccessEvaluator(source);

        Assert.True(evaluator.EvaluatePage(Group("42"), 10, Index(source)).GrantsRead);
        Assert.False(evaluator.EvaluatePage(UserIn("7"), 10, Index(source)).GrantsRead);
        Assert.False(evaluator.EvaluatePage(Role(SecurityAccount.AnonymousRole), 10, Index(source)).GrantsRead);
    }

    [Fact]
    public void BareGroupGrant_IsSilentlyOverriddenByRoleDefault()
    {
        // The classic trap: only a group grant, no broad-role deny -> everyone still sees it.
        var source = SourceWithPages(P(10));
        source.Rows.Add(new ContentPermissionRow("42", ContentEntityNames.Page, "10", Levels.Read));
        var evaluator = new EffectiveAccessEvaluator(source);

        var outsider = evaluator.EvaluatePage(UserIn("7"), 10, Index(source));

        Assert.True(outsider.GrantsRead);
        Assert.Equal(AccessOrigin.RoleDefault, outsider.Origin);
        Assert.Equal(SecurityAccount.AuthenticatedFrontendRole, outsider.WinningOwnerId);
    }

    [Fact]
    public void HighestWins_AcrossGroups()
    {
        var source = SourceWithPages(P(10));
        source.Rows.Add(new ContentPermissionRow(SecurityAccount.AuthenticatedFrontendRole, ContentEntityNames.Page, "10", Levels.None));
        source.Rows.Add(new ContentPermissionRow("7", ContentEntityNames.Page, "10", Levels.None));
        source.Rows.Add(new ContentPermissionRow("42", ContentEntityNames.Page, "10", Levels.Edit));
        var evaluator = new EffectiveAccessEvaluator(source);

        var access = evaluator.EvaluatePage(UserIn("7", "42"), 10, Index(source));

        Assert.Equal(Levels.Edit, access.Level);
        Assert.Equal("42", access.WinningOwnerId);
    }

    [Fact]
    public void Inheritance_WalksToNearestAncestorWithRows()
    {
        var source = SourceWithPages(P(10), P(11, parentId: 10), P(12, parentId: 11));
        source.Rows.Add(new ContentPermissionRow(SecurityAccount.AuthenticatedFrontendRole, ContentEntityNames.Page, "10", Levels.None));
        source.Rows.Add(new ContentPermissionRow(SecurityAccount.AnonymousRole, ContentEntityNames.Page, "10", Levels.None));
        source.Rows.Add(new ContentPermissionRow("42", ContentEntityNames.Page, "10", Levels.Read));
        var evaluator = new EffectiveAccessEvaluator(source);

        var deep = evaluator.EvaluatePage(UserIn("99"), 12, Index(source));

        Assert.False(deep.GrantsRead);
        Assert.Equal(AccessOrigin.InheritedFromPage, deep.Origin);
        Assert.Equal(10, deep.OriginPageId);
    }

    [Fact]
    public void NearestRowsetWins_OverridesAncestorGate()
    {
        // Child carries its own rows re-opening access; the ancestor deny no longer applies.
        var source = SourceWithPages(P(10), P(11, parentId: 10));
        source.Rows.Add(new ContentPermissionRow(SecurityAccount.AuthenticatedFrontendRole, ContentEntityNames.Page, "10", Levels.None));
        source.Rows.Add(new ContentPermissionRow(SecurityAccount.AuthenticatedFrontendRole, ContentEntityNames.Page, "11", Levels.Read));
        var evaluator = new EffectiveAccessEvaluator(source);

        var child = evaluator.EvaluatePage(UserIn(), 11, Index(source));

        Assert.True(child.GrantsRead);
        Assert.Equal(AccessOrigin.ExplicitHere, child.Origin);
    }

    [Fact]
    public void AdminBypass_AlwaysAll()
    {
        var source = SourceWithPages(P(10));
        source.Rows.Add(new ContentPermissionRow(SecurityAccount.AuthenticatedFrontendRole, ContentEntityNames.Page, "10", Levels.None));
        source.Rows.Add(new ContentPermissionRow(SecurityAccount.AnonymousRole, ContentEntityNames.Page, "10", Levels.None));
        var evaluator = new EffectiveAccessEvaluator(source);

        var access = evaluator.EvaluatePage(Admin(), 10, Index(source));

        Assert.Equal(Levels.All, access.Level);
        Assert.Equal(AccessOrigin.Bypass, access.Origin);
    }

    [Fact]
    public void Paragraph_WithoutOwnRows_FollowsPage()
    {
        var source = SourceWithPages(P(10));
        source.Paragraphs.Add(new ParagraphNode(500, 10, "Tile", ""));
        source.Rows.Add(new ContentPermissionRow(SecurityAccount.AuthenticatedFrontendRole, ContentEntityNames.Page, "10", Levels.None));
        var evaluator = new EffectiveAccessEvaluator(source);

        var pageAccess = evaluator.EvaluatePage(UserIn(), 10, Index(source));
        var paragraph = evaluator.EvaluateParagraph(UserIn(), 500, pageAccess);

        Assert.False(paragraph.GrantsRead);
        Assert.Equal(AccessOrigin.PageFallback, paragraph.Origin);
    }

    [Fact]
    public void Paragraph_WithDenyGrantPair_PersonalisesTiles()
    {
        // Two personas, one shared page, per-paragraph gating: each sees only their tile.
        var source = SourceWithPages(P(10));
        source.Paragraphs.Add(new ParagraphNode(500, 10, "Buyer tile", ""));
        source.Paragraphs.Add(new ParagraphNode(501, 10, "CSR tile", ""));
        foreach (var (paragraphId, groupId) in new[] { ("500", "42"), ("501", "43") })
        {
            source.Rows.Add(new ContentPermissionRow(SecurityAccount.AuthenticatedFrontendRole, ContentEntityNames.Paragraph, paragraphId, Levels.None));
            source.Rows.Add(new ContentPermissionRow(SecurityAccount.AnonymousRole, ContentEntityNames.Paragraph, paragraphId, Levels.None));
            source.Rows.Add(new ContentPermissionRow(groupId, ContentEntityNames.Paragraph, paragraphId, Levels.All));
        }
        var evaluator = new EffectiveAccessEvaluator(source);

        var buyer = UserIn("42");
        var pageAccess = evaluator.EvaluatePage(buyer, 10, Index(source));

        Assert.True(evaluator.EvaluateParagraph(buyer, 500, pageAccess).GrantsRead);
        Assert.False(evaluator.EvaluateParagraph(buyer, 501, pageAccess).GrantsRead);
    }
}
