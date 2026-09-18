using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.BackendRights.BackendRightsTestData;
using Truvio.Commerce.PowerTools.Features.BackendRights.Core;
using Truvio.Commerce.PowerTools.Shared.Permissions;

namespace Truvio.Commerce.PowerTools.Tests.Features.BackendRights;

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
