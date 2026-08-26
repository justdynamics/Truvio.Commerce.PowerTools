using Truvio.Commerce.PowerTools.Core.Permissions;
using Truvio.Commerce.PowerTools.Core.Rights;
using Xunit;
using static Truvio.Commerce.PowerTools.Tests.BackendRightsTestData;

namespace Truvio.Commerce.PowerTools.Tests;

/// <summary>The gate-exclusivity matrix — the rule the whole tool exists to show.</summary>
public class GateExclusivityTests
{
    [Fact]
    public void FlagOff_PermissionDecides_EvenWhenAreaDeclaresCapability()
    {
        var snapshot = Snapshot(
            capabilityControlActive: false,
            nodes: [Area(capability: ContentKey, level: Levels.Read)],
            limitations: [Limit(ContentKey)]);

        var verdict = For(snapshot, "Content");

        // The limitation exists but the feature is off, so it restricts nobody.
        Assert.True(verdict.Visible);
        Assert.Equal(RightsGate.Permission, verdict.DecidedBy);
        Assert.True(verdict.PermissionConsulted);
        Assert.False(verdict.CapabilityConsulted);
    }

    [Fact]
    public void FlagOn_WithCapability_CapabilityDecides_AndPermissionIsSkipped()
    {
        var snapshot = Snapshot(
            capabilityControlActive: true,
            nodes: [Area(capability: ContentKey, level: Levels.Read)],
            limitations: [Limit(ContentKey)]);

        var verdict = For(snapshot, "Content");

        Assert.False(verdict.Visible);
        Assert.Equal(RightsGate.Capability, verdict.DecidedBy);
        Assert.True(verdict.CapabilityConsulted);
        Assert.False(verdict.PermissionConsulted);
        // Read is granted, and it makes no difference — that is the point of the report.
        Assert.True(verdict.PermissionGrantsRead);
    }

    [Fact]
    public void FlagOn_WithoutCapability_PermissionStillDecides()
    {
        var snapshot = Snapshot(
            capabilityControlActive: true,
            nodes: [Area(name: "Settings", capability: string.Empty, level: Levels.NotSet)]);

        var verdict = For(snapshot, "Settings");

        Assert.False(verdict.Visible);
        Assert.Equal(RightsGate.Permission, verdict.DecidedBy);
        Assert.True(verdict.PermissionConsulted);
    }

    [Fact]
    public void PermissionWithoutRead_HidesTheArea()
    {
        var snapshot = Snapshot(nodes: [Area(level: Levels.None)]);

        Assert.False(For(snapshot, "Content").Visible);
    }

    [Fact]
    public void LicenseGateAppliesOnTopOfAPassingGate()
    {
        var snapshot = Snapshot(nodes: [Area(level: Levels.Read, licenseFeature: "Insights", licenseOk: false)]);

        var verdict = For(snapshot, "Content");

        Assert.False(verdict.Visible);
        Assert.Equal(RightsGate.License, verdict.DecidedBy);
    }
}

/// <summary>Cascade must follow RequiredCapabilities, never the key string.</summary>
public class CapabilityCascadeTests
{
    [Fact]
    public void RestrictingAParent_CascadesToTheChildThatRequiresIt()
    {
        var snapshot = Snapshot(
            capabilityControlActive: true,
            nodes: [Area(capability: ContentKey), Section(capability: NavigationKey)],
            limitations: [Limit(ContentKey)]);

        var child = RightsEvaluator.ResolveCapability(snapshot, NavigationKey);

        Assert.True(child.Restricted);
        Assert.Equal(CapabilityCause.Cascaded, child.Cause);
        Assert.Equal(ContentKey, child.CausingKey);
        Assert.Contains("Editors", child.CausingGroups);
    }

    [Fact]
    public void StringPrefixIsNotTheHierarchy_SettingsIsNotAChildOfContent()
    {
        // DW ships /Content/Settings with NO required capability — restricting /Content must not
        // touch it, even though the key looks nested.
        var snapshot = Snapshot(
            capabilityControlActive: true,
            limitations: [Limit(ContentKey)]);

        var settings = RightsEvaluator.ResolveCapability(snapshot, SettingsKey);

        Assert.False(settings.Restricted);
    }

    [Fact]
    public void DirectRestrictionNamesTheGroup()
    {
        var snapshot = Snapshot(
            capabilityControlActive: true,
            limitations: [Limit(ContentKey, groupName: "Editors")]);

        var verdict = RightsEvaluator.ResolveCapability(snapshot, ContentKey);

        Assert.Equal(CapabilityCause.Direct, verdict.Cause);
        Assert.Equal(["Editors"], verdict.CausingGroups);
    }

    [Fact]
    public void LimitationOnAGroupTheUserIsNotIn_DoesNotApply()
    {
        var snapshot = Snapshot(
            capabilityControlActive: true,
            limitations: [Limit(ContentKey, groupId: 99)],
            groupIds: [42]);

        Assert.False(RightsEvaluator.ResolveCapability(snapshot, ContentKey).Restricted);
    }

    [Fact]
    public void AnyOneGroupRestricting_IsEnough()
    {
        var snapshot = Snapshot(
            capabilityControlActive: true,
            limitations: [Limit(ContentKey, groupId: 7, groupName: "Interns")],
            groupIds: [42, 7]);

        Assert.True(RightsEvaluator.ResolveCapability(snapshot, ContentKey).Restricted);
    }

    [Fact]
    public void UnknownKey_LimitsNobody()
    {
        var snapshot = Snapshot(
            capabilityControlActive: true,
            limitations: [Limit("/Ghost")],
            capabilities: Capabilities());

        Assert.False(RightsEvaluator.ResolveCapability(snapshot, "/Ghost").Restricted);
    }

    [Fact]
    public void WithoutCapabilityData_TheVerdictIsUnknown_NotDenied()
    {
        var snapshot = Snapshot(capabilityControlActive: true, capabilityDataAvailable: false);

        var verdict = RightsEvaluator.ResolveCapability(snapshot, ContentKey);

        Assert.False(verdict.Restricted);
        Assert.Equal(CapabilityCause.Unknown, verdict.Cause);
    }
}

/// <summary>The three verdict classes, and the asymmetry between them.</summary>
public class SubjectVerdictTests
{
    [Fact]
    public void NoBackendAccess_HidesEverything_WhateverIsGranted()
    {
        var snapshot = Snapshot(
            subject: Standard(allowBackend: false),
            nodes: [Area(level: Levels.All)]);

        Assert.False(For(snapshot, "Content").Visible);
    }

    [Fact]
    public void BuiltInAdmin_BypassesBothGates()
    {
        var snapshot = Snapshot(
            subject: BuiltInAdmin(),
            capabilityControlActive: true,
            nodes: [Area(level: Levels.NotSet, capability: ContentKey)],
            limitations: [Limit(ContentKey)]);

        var verdict = For(snapshot, "Content");

        Assert.True(verdict.Visible);
        Assert.Equal(RightsGate.Bypass, verdict.DecidedBy);
    }

    [Fact]
    public void Angel_BypassesBothGates()
    {
        var snapshot = Snapshot(
            subject: Angel(),
            capabilityControlActive: true,
            nodes: [Area(level: Levels.NotSet, capability: ContentKey)],
            limitations: [Limit(ContentKey)]);

        Assert.True(For(snapshot, "Content").Visible);
    }

    [Fact]
    public void AdministratorUserType_IsStillCapabilityRestricted()
    {
        // The footgun: IsAdmin bypasses permissions (via the role default) but NOT capabilities.
        var snapshot = Snapshot(
            subject: Administrator(),
            capabilityControlActive: true,
            nodes: [Area(level: Levels.All, capability: ContentKey)],
            limitations: [Limit(ContentKey)]);

        var verdict = For(snapshot, "Content");

        Assert.False(verdict.Visible);
        Assert.Equal(RightsGate.Capability, verdict.DecidedBy);
    }

    [Fact]
    public void ElevatedStillLosesToTheLicenseGate()
    {
        var snapshot = Snapshot(
            subject: BuiltInAdmin(),
            nodes: [Area(licenseFeature: "Insights", licenseOk: false)]);

        var verdict = For(snapshot, "Content");

        Assert.False(verdict.Visible);
        Assert.Equal(RightsGate.License, verdict.DecidedBy);
    }
}

/// <summary>Sections and nodes gate differently from areas — verified against NavigationByPathQuery.</summary>
public class TreeDepthTests
{
    [Fact]
    public void SectionUnderAHiddenArea_IsUnreachable()
    {
        var snapshot = Snapshot(
            nodes: [Area(level: Levels.None), Section(level: Levels.Read)]);

        Assert.False(For(snapshot, "Navigation").Visible);
    }

    [Fact]
    public void WithFlagOn_SectionWithoutCapability_IsNotPermissionChecked()
    {
        // GetSectionResult only calls ProcessPermissions() while the flag is OFF, and even then it
        // filters the section's children — never the section itself.
        var snapshot = Snapshot(
            capabilityControlActive: true,
            nodes: [Area(level: Levels.Read, capability: ContentKey), Section(capability: string.Empty, level: Levels.None)]);

        var verdict = For(snapshot, "Navigation");

        Assert.True(verdict.Visible);
        Assert.Equal(RightsGate.None, verdict.DecidedBy);
        Assert.False(verdict.PermissionConsulted);
    }

    [Fact]
    public void NodeRequiringMoreThanTheParentGrants_IsDropped()
    {
        var snapshot = Snapshot(
            nodes:
            [
                Area(level: Levels.Read),
                Section(level: Levels.Read),
                Node(level: Levels.Read, requiredLevel: Levels.Create)
            ]);

        Assert.False(For(snapshot, "Pages").Visible);
    }

    [Fact]
    public void NodeWithinTheParentsLevel_IsKept()
    {
        var snapshot = Snapshot(
            nodes:
            [
                Area(level: Levels.All),
                Section(level: Levels.All),
                Node(level: Levels.Read, requiredLevel: Levels.Read)
            ]);

        Assert.True(For(snapshot, "Pages").Visible);
    }

    [Fact]
    public void NodeWithoutOwnLevel_IsJudgedByTheParents()
    {
        // Most node types never set PermissionLevelCurrentUser; DW gates a node on its parent's
        // level, so a missing own level is not a denial.
        var snapshot = Snapshot(
            nodes:
            [
                Area(level: Levels.All),
                Section(level: Levels.All),
                Node(level: null)
            ]);

        var verdict = For(snapshot, "Pages");

        Assert.True(verdict.Visible);
        Assert.True(verdict.PermissionConsulted);
    }

    [Fact]
    public void NodeWithoutOwnLevelOrCapability_UnderCapabilityControl_UsesTheParentsLevel()
    {
        // The Administrator screenshot case: Insights children declare no capability and carry no
        // own level — the role-default All on the area must still let them through.
        var snapshot = Snapshot(
            subject: Administrator(),
            capabilityControlActive: true,
            nodes:
            [
                Area(level: Levels.All, origin: PermissionOrigin.RoleDefault),
                Section(level: Levels.All, capability: string.Empty),
                Node(level: null)
            ]);

        Assert.True(For(snapshot, "Pages").Visible);
    }

    [Fact]
    public void NodeWithoutOwnLevel_UnderAParentWithoutRead_StaysHidden()
    {
        // The fallback inherits the parent's level, it does not blanket-grant.
        var snapshot = Snapshot(
            capabilityControlActive: true,
            nodes:
            [
                Area(level: Levels.None, origin: PermissionOrigin.ContextDefault),
                Section(level: Levels.None, capability: string.Empty),
                Node(level: null)
            ]);

        Assert.False(For(snapshot, "Pages").Visible);
    }

    [Fact]
    public void AreaCount_CountsAreasOnly()
    {
        var snapshot = Snapshot(
            nodes:
            [
                Area(name: "Content", level: Levels.Read),
                Area(name: "Commerce", level: Levels.None),
                Section(),
                Node()
            ]);

        var (visible, total) = RightsEvaluator.AreaCount(RightsEvaluator.Evaluate(snapshot));

        Assert.Equal(1, visible);
        Assert.Equal(2, total);
    }
}

/// <summary>DW's own answer versus ours — reported, never silently resolved.</summary>
public class DisagreementTests
{
    [Fact]
    public void DwSaysRestrictedButNoRowExplainsIt_IsReported()
    {
        var snapshot = Snapshot(
            capabilityControlActive: true,
            nodes: [Area(capability: ContentKey, dwSaysRestricted: true)]);

        var verdict = For(snapshot, "Content");

        Assert.True(verdict.HasDisagreement);
        Assert.Contains("no limitation row explains it", verdict.Disagreement);
    }

    [Fact]
    public void AgreementIsSilent()
    {
        var snapshot = Snapshot(
            capabilityControlActive: true,
            nodes: [Area(capability: ContentKey, dwSaysRestricted: true)],
            limitations: [Limit(ContentKey)]);

        Assert.False(For(snapshot, "Content").HasDisagreement);
    }
}

/// <summary>The Why? wording — it is the product, so it is tested like one.</summary>
public class RightsExplanationTests
{
    [Fact]
    public void CapabilityDenial_NamesTheGroupAndTheFix()
    {
        var snapshot = Snapshot(
            capabilityControlActive: true,
            nodes: [Area(capability: ContentKey, level: Levels.Read)],
            limitations: [Limit(ContentKey, groupName: "Editors")]);

        var sentences = RightsExplanation.Sentences(snapshot, For(snapshot, "Content"));
        var text = string.Join(" ", sentences);

        Assert.Contains("Group 'Editors' restricts it", text);
        Assert.Contains("Removing that limitation", text);
        // And it must say the granted permission is dead today.
        Assert.Contains("not consulted", text);
    }

    [Fact]
    public void CascadedDenial_SaysThereIsNoRowOfItsOwn()
    {
        var snapshot = Snapshot(
            capabilityControlActive: true,
            nodes: [Area(name: "Nav", capability: NavigationKey)],
            limitations: [Limit(ContentKey)]);

        var text = string.Join(" ", RightsExplanation.Sentences(snapshot, For(snapshot, "Nav")));

        Assert.Contains("There is no limitation on /Content/Navigation itself", text);
    }

    [Fact]
    public void NoGrant_ExplainsThatBackendAccessIsGrantOnly()
    {
        var snapshot = Snapshot(nodes: [Area(name: "Commerce", level: Levels.NotSet, capability: string.Empty)]);

        var text = string.Join(" ", RightsExplanation.Sentences(snapshot, For(snapshot, "Commerce")));

        Assert.Contains("declares no default", text);
        Assert.Contains("grant-only", text);
    }

    [Fact]
    public void AdministratorType_IsWarnedAboutCapabilities()
    {
        var snapshot = Snapshot(
            subject: Administrator(),
            nodes: [Area(level: Levels.All, origin: PermissionOrigin.RoleDefault, capability: string.Empty)]);

        var text = string.Join(" ", RightsExplanation.Sentences(snapshot, For(snapshot, "Content")));

        Assert.Contains("Capability limitations still apply", text);
    }

    [Fact]
    public void NoBackendAccess_SaysSoFirst()
    {
        var snapshot = Snapshot(subject: Standard(allowBackend: false), nodes: [Area(level: Levels.All)]);

        var sentences = RightsExplanation.Sentences(snapshot, For(snapshot, "Content"));

        Assert.Contains("no backend access", sentences[0]);
    }

    [Fact]
    public void InheritedPermission_NamesTheParentSection()
    {
        var area = Area(name: "Content", level: Levels.Read, capability: string.Empty,
            origin: PermissionOrigin.Inherited) with
        { InheritedFrom = "Root" };

        var text = string.Join(" ", RightsExplanation.Sentences(Snapshot(nodes: [area]), For(Snapshot(nodes: [area]), "Content")));

        Assert.Contains("inherits Read from section 'Root'", text);
    }
}
