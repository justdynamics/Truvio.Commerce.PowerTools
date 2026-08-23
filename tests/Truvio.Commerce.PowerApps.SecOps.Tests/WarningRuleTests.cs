using Truvio.Commerce.PowerApps.SecOps.Core.Diagnostics;
using Truvio.Commerce.PowerApps.SecOps.Core.Diagnostics.Rules;
using Truvio.Commerce.PowerApps.SecOps.Core.Permissions;
using Truvio.Commerce.PowerApps.SecOps.Core.Principals;
using Xunit;

namespace Truvio.Commerce.PowerApps.SecOps.Tests;

public class WarningRuleTests
{
    private static FakeContentSecuritySource BaseSource()
    {
        var source = new FakeContentSecuritySource();
        source.Areas.Add(new AreaNode(1, "Site"));
        source.Pages.Add(new PageNode(10, 0, 1, "Home", 1, true, false));
        source.Pages.Add(new PageNode(11, 0, 1, "Members", 2, true, false));
        source.GroupIds.Add("42");
        return source;
    }

    private static IReadOnlyList<Finding> Run(IWarningRule rule, FakeContentSecuritySource source) =>
        rule.Evaluate(new WarningContext(source)).ToList();

    [Fact]
    public void BareGroupGrant_WithoutBroadDeny_IsCritical()
    {
        var source = BaseSource();
        source.Rows.Add(new ContentPermissionRow("42", ContentEntityNames.Page, "11", Levels.Read));

        var findings = Run(new BareGroupGrantRule(), source);

        var finding = Assert.Single(findings);
        Assert.Equal(FindingSeverity.Critical, finding.Severity);
        Assert.Contains("Members", finding.EntityDisplayName);
    }

    [Fact]
    public void GroupGrant_WithAuthenticatedDenyButNoAnonymousDeny_WarnsAboutAnonymous()
    {
        var source = BaseSource();
        source.Rows.Add(new ContentPermissionRow("42", ContentEntityNames.Page, "11", Levels.Read));
        source.Rows.Add(new ContentPermissionRow(SecurityAccount.AuthenticatedFrontendRole, ContentEntityNames.Page, "11", Levels.None));

        var findings = Run(new BareGroupGrantRule(), source);

        var finding = Assert.Single(findings);
        Assert.Equal(FindingSeverity.Warning, finding.Severity);
        Assert.Contains("Anonymous", finding.Title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProperDenyGrantPair_ProducesNoFindings()
    {
        var source = BaseSource();
        source.Rows.Add(new ContentPermissionRow("42", ContentEntityNames.Page, "11", Levels.Read));
        source.Rows.Add(new ContentPermissionRow(SecurityAccount.AuthenticatedFrontendRole, ContentEntityNames.Page, "11", Levels.None));
        source.Rows.Add(new ContentPermissionRow(SecurityAccount.AnonymousRole, ContentEntityNames.Page, "11", Levels.None));

        Assert.Empty(Run(new BareGroupGrantRule(), source));
    }

    [Fact]
    public void GatedLoginPage_IsCritical()
    {
        var source = BaseSource();
        source.Paragraphs.Add(new ParagraphNode(500, 10, "Sign in", "UserAuthentication"));
        source.Rows.Add(new ContentPermissionRow(SecurityAccount.AnonymousRole, ContentEntityNames.Page, "10", Levels.None));

        var findings = Run(new GatedLoginPageRule(), source);

        var finding = Assert.Single(findings);
        Assert.Equal(FindingSeverity.Critical, finding.Severity);
        Assert.Equal("10", finding.EntityKey);
    }

    [Fact]
    public void UngatedLoginPage_ProducesNoFindings()
    {
        var source = BaseSource();
        source.Paragraphs.Add(new ParagraphNode(500, 10, "Sign in", "UserAuthentication"));

        Assert.Empty(Run(new GatedLoginPageRule(), source));
    }

    [Fact]
    public void LegacyColumns_AreReported()
    {
        var source = BaseSource();
        source.LegacyPageIds.Add(10);
        source.LegacyParagraphIds.Add(500);
        source.Paragraphs.Add(new ParagraphNode(500, 10, "Old gated", ""));

        var findings = Run(new LegacyPermissionColumnRule(), source);

        Assert.Equal(2, findings.Count);
        Assert.All(findings, f => Assert.Equal(FindingSeverity.Warning, f.Severity));
    }

    [Fact]
    public void OrphanedGroupRow_IsReported_ExistingGroupIsNot()
    {
        var source = BaseSource();
        source.Rows.Add(new ContentPermissionRow("42", ContentEntityNames.Page, "10", Levels.Read));  // exists
        source.Rows.Add(new ContentPermissionRow("77", ContentEntityNames.Page, "10", Levels.Read));  // deleted
        source.Rows.Add(new ContentPermissionRow(SecurityAccount.AnonymousRole, ContentEntityNames.Page, "10", Levels.None)); // role, never orphaned

        var findings = Run(new OrphanedGrantRule(), source);

        var finding = Assert.Single(findings);
        Assert.Contains("77", finding.Detail);
    }

    [Fact]
    public void WarningEngine_OrdersCriticalFirst()
    {
        var source = BaseSource();
        source.Rows.Add(new ContentPermissionRow("42", ContentEntityNames.Page, "11", Levels.Read)); // W1 critical
        source.Rows.Add(new ContentPermissionRow("77", ContentEntityNames.Page, "10", Levels.Read)); // W4 info

        var findings = new WarningEngine().Run(source);

        Assert.True(findings.Count >= 2);
        Assert.Equal(FindingSeverity.Critical, findings[0].Severity);
    }
}
