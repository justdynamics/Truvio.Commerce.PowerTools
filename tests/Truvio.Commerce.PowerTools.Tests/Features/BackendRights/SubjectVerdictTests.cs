using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.BackendRights.BackendRightsTestData;
using Truvio.Commerce.PowerTools.Features.BackendRights.Core;
using Truvio.Commerce.PowerTools.Shared.Permissions;

namespace Truvio.Commerce.PowerTools.Tests.Features.BackendRights;

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
