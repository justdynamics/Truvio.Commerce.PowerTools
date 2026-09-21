using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.BackendRights.BackendRightsTestData;
using Truvio.Commerce.PowerTools.Features.BackendRights.Core;
using Truvio.Commerce.PowerTools.Shared.Permissions;

namespace Truvio.Commerce.PowerTools.Tests.Features.BackendRights;

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
