using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.BackendRights.BackendRightsTestData;
using Truvio.Commerce.PowerTools.Features.BackendRights.Core;

namespace Truvio.Commerce.PowerTools.Tests.Features.BackendRights;

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
