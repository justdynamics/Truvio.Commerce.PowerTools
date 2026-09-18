using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.PimQuality.PimTestData;
using Truvio.Commerce.PowerTools.Features.PimQuality.Core;

namespace Truvio.Commerce.PowerTools.Tests.Features.PimQuality;

// ---- The snapshot's own arithmetic ---------------------------------------------------------------------

public class PimSnapshotTests
{
    [Fact]
    public void Truncation_is_derived_from_the_total_count()
    {
        var snapshot = Snapshot(products: [Product()], totalProductCount: 900);

        Assert.True(snapshot.IsTruncated);
        Assert.Equal(899, snapshot.NotShownCount);
    }

    [Fact]
    public void A_complete_scan_is_not_truncated()
    {
        var snapshot = Snapshot(products: [Product()], totalProductCount: 1);

        Assert.False(snapshot.IsTruncated);
        Assert.Equal(0, snapshot.NotShownCount);
    }

    [Fact]
    public void Average_of_an_empty_catalog_is_zero_not_a_crash()
    {
        Assert.Equal(0, Snapshot().AverageScore);
    }

    [Fact]
    public void Scope_falls_back_when_the_cap_is_nonsense()
    {
        Assert.Equal(PimScope.DefaultProductCap, new PimScope(ProductCap: 0).EffectiveCap);
        Assert.Equal(PimScope.DefaultProductCap, new PimScope(ProductCap: -5).EffectiveCap);
        Assert.Equal(50, new PimScope(ProductCap: 50).EffectiveCap);
    }
}
