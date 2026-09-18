using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.BackendRights.BackendRightsTestData;
using Truvio.Commerce.PowerTools.Features.BackendRights.Core;
using Truvio.Commerce.PowerTools.Shared.Permissions;

namespace Truvio.Commerce.PowerTools.Tests.Features.BackendRights;

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
