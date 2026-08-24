using Truvio.Commerce.PowerTools.Core.Diagnostics;
using Truvio.Commerce.PowerTools.Core.Permissions;
using Truvio.Commerce.PowerTools.Core.Rights;
using Truvio.Commerce.PowerTools.Core.Rights.Rules;
using Xunit;
using static Truvio.Commerce.PowerTools.Tests.BackendRightsTestData;

namespace Truvio.Commerce.PowerTools.Tests;

/// <summary>SECOPS-B1..B6 — the latent-misconfiguration rules that ride along in Content Access Warnings.</summary>
public class BackendRightsRuleTests
{
    private static IReadOnlyList<Finding> Run(RightsSnapshot snapshot) => new RightsWarningEngine().Run(snapshot);

    // ---- B1 ------------------------------------------------------------------------------------

    [Fact]
    public void B1_LimitationsStoredWhileFeatureOff_IsInfo()
    {
        var findings = Run(Snapshot(capabilityControlActive: false, limitations: [Limit(ContentKey)]));

        var finding = Assert.Single(findings, f => f.RuleId == InactiveCapabilityLimitationRule.Id);
        Assert.Equal(FindingSeverity.Info, finding.Severity);
        Assert.Contains("no effect today", finding.Detail);
    }

    [Fact]
    public void B1_Silent_WhenFeatureIsOn()
    {
        var findings = Run(Snapshot(capabilityControlActive: true, limitations: [Limit(ContentKey)]));

        Assert.DoesNotContain(findings, f => f.RuleId == InactiveCapabilityLimitationRule.Id);
    }

    // ---- B2 ------------------------------------------------------------------------------------

    [Fact]
    public void B2_LimitationOnAnUndeclaredKey_IsWarning()
    {
        var findings = Run(Snapshot(capabilityControlActive: true, limitations: [Limit("/Ghost")]));

        var finding = Assert.Single(findings, f => f.RuleId == UnknownCapabilityKeyRule.Id);
        Assert.Equal(FindingSeverity.Warning, finding.Severity);
        Assert.Equal("/Ghost", finding.EntityKey);
    }

    [Fact]
    public void B2_Silent_ForDeclaredKeys()
    {
        var findings = Run(Snapshot(capabilityControlActive: true, limitations: [Limit(ContentKey)]));

        Assert.DoesNotContain(findings, f => f.RuleId == UnknownCapabilityKeyRule.Id);
    }

    // ---- B3 ------------------------------------------------------------------------------------

    [Fact]
    public void B3_LimitationOwnedByADeletedGroup_IsWarning()
    {
        var findings = Run(Snapshot(
            capabilityControlActive: true,
            limitations: [new CapabilityLimitationSpec(99, string.Empty, ContentKey)]));

        var finding = Assert.Single(findings, f => f.RuleId == DeletedLimitationGroupRule.Id);
        Assert.Contains("no longer exists", finding.Detail);
    }

    // ---- B4 ------------------------------------------------------------------------------------

    [Fact]
    public void B4_SectionRowsWithNoLiveArea_ExplainTheRenameTrap()
    {
        var findings = Run(Snapshot(orphanedSectionKeys: ["Old area name"]));

        var finding = Assert.Single(findings, f => f.RuleId == OrphanedSectionPermissionRule.Id);
        Assert.Contains("DISPLAY NAME", finding.Detail);
    }

    // ---- B5 ------------------------------------------------------------------------------------

    [Fact]
    public void B5_BackendUserWithNoVisibleArea_IsWarned()
    {
        var findings = Run(Snapshot(nodes: [Area(level: Levels.None, capability: string.Empty)]));

        var finding = Assert.Single(findings, f => f.RuleId == NoVisibleAreaRule.Id);
        Assert.Contains("empty shell", finding.Detail);
    }

    [Fact]
    public void B5_Silent_WhenSomethingIsVisible()
    {
        var findings = Run(Snapshot(nodes: [Area(level: Levels.Read, capability: string.Empty)]));

        Assert.DoesNotContain(findings, f => f.RuleId == NoVisibleAreaRule.Id);
    }

    [Fact]
    public void B5_Silent_WhenTheUserCannotReachTheBackendAtAll()
    {
        // Not a misconfiguration: the account is simply not a backend account.
        var findings = Run(Snapshot(
            subject: Standard(allowBackend: false),
            nodes: [Area(level: Levels.None, capability: string.Empty)]));

        Assert.DoesNotContain(findings, f => f.RuleId == NoVisibleAreaRule.Id);
    }

    // ---- B6 ------------------------------------------------------------------------------------

    [Fact]
    public void B6_BothGatesConfigured_ReportsTheDeadOne()
    {
        var findings = Run(Snapshot(
            capabilityControlActive: true,
            nodes: [Area(capability: ContentKey, level: Levels.Read, origin: PermissionOrigin.Explicit)]));

        var finding = Assert.Single(findings, f => f.RuleId == DeadGateConfigurationRule.Id);
        Assert.Equal(FindingSeverity.Info, finding.Severity);
        Assert.Contains("dead configuration", finding.Detail);
    }

    [Fact]
    public void B6_Silent_WhenTheFlagIsOff()
    {
        var findings = Run(Snapshot(
            capabilityControlActive: false,
            nodes: [Area(capability: ContentKey, origin: PermissionOrigin.Explicit)]));

        Assert.DoesNotContain(findings, f => f.RuleId == DeadGateConfigurationRule.Id);
    }

    // ---- Engine ---------------------------------------------------------------------------------

    [Fact]
    public void HealthySolution_ProducesNothing()
    {
        var findings = Run(Snapshot(nodes: [Area(level: Levels.Read, capability: string.Empty)]));

        Assert.Empty(findings);
    }

    [Fact]
    public void ARuleThatThrows_DoesNotHideTheOthers()
    {
        var engine = new RightsWarningEngine([new ThrowingRule(), new NoVisibleAreaRule()]);

        var findings = engine.Run(Snapshot(nodes: [Area(level: Levels.None, capability: string.Empty)]));

        Assert.Contains(findings, f => f.RuleId == "SECOPS-BE");
        Assert.Contains(findings, f => f.RuleId == NoVisibleAreaRule.Id);
    }

    [Fact]
    public void FindingsAreOrderedWorstFirst()
    {
        var findings = Run(Snapshot(
            capabilityControlActive: false,
            nodes: [Area(level: Levels.None, capability: string.Empty)],
            limitations: [Limit(ContentKey)]));

        // B5 is a Warning, B1 an Info.
        Assert.Equal(FindingSeverity.Warning, findings[0].Severity);
        Assert.Equal(FindingSeverity.Info, findings[^1].Severity);
    }

    private sealed class ThrowingRule : IRightsRule
    {
        public string RuleId => "SECOPS-B0";

        public IEnumerable<Finding> Evaluate(RightsSnapshot snapshot) => throw new InvalidOperationException("boom");
    }
}
