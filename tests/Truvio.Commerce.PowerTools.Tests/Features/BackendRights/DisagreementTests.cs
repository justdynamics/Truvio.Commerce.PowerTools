using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.BackendRights.BackendRightsTestData;

namespace Truvio.Commerce.PowerTools.Tests.Features.BackendRights;

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
