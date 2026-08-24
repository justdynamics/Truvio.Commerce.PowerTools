using Truvio.Commerce.PowerTools.Core.Commerce;
using Xunit;

namespace Truvio.Commerce.PowerTools.Tests;

public class PdpPreviewTests
{
    [Fact]
    public void ExplicitShopMapping_Wins()
    {
        Assert.Equal(1234, PdpPreview.ResolvePageId("SHOP40=1234\nSHOP30=5678", "SHOP40"));
        Assert.Equal(5678, PdpPreview.ResolvePageId("SHOP40=1234\nSHOP30=5678", "SHOP30"));
    }

    [Fact]
    public void ShopMatch_IsCaseInsensitive()
    {
        Assert.Equal(1234, PdpPreview.ResolvePageId("shop40=1234", "SHOP40"));
    }

    [Fact]
    public void BarePageId_IsTheDefault_ForUnmappedShops()
    {
        Assert.Equal(70, PdpPreview.ResolvePageId("SHOP40=1234\n70", "SHOP99"));
        Assert.Equal(70, PdpPreview.ResolvePageId("70", null));
        Assert.Equal(70, PdpPreview.ResolvePageId("70", ""));
    }

    [Fact]
    public void MappingBeatsDefault_DefaultCoversNoShop()
    {
        var setting = "70\nSHOP40=1234";
        Assert.Equal(1234, PdpPreview.ResolvePageId(setting, "SHOP40"));
        Assert.Equal(70, PdpPreview.ResolvePageId(setting, "SHOP30"));
    }

    [Fact]
    public void GarbageLines_AreIgnored()
    {
        Assert.Null(PdpPreview.ResolvePageId("not-a-number\nSHOP40=abc\n=12\nSHOP30=0", "SHOP40"));
        Assert.Null(PdpPreview.ResolvePageId("   \n\n", "SHOP40"));
        Assert.Null(PdpPreview.ResolvePageId(null, "SHOP40"));
    }

    [Fact]
    public void SeparatorsAndWhitespace_AreTolerated()
    {
        Assert.Equal(1234, PdpPreview.ResolvePageId("  SHOP40 = 1234 ; SHOP30=5678 ", "SHOP40"));
        Assert.Equal(5678, PdpPreview.ResolvePageId("SHOP40=1234,SHOP30=5678", "SHOP30"));
    }

    [Fact]
    public void Url_CarriesProductAndVariant_Encoded()
    {
        Assert.Equal("/Default.aspx?ID=70&ProductID=PROD1", PdpPreview.BuildUrl(70, "PROD1"));
        Assert.Equal("/Default.aspx?ID=70&ProductID=PROD1&VariantID=V1", PdpPreview.BuildUrl(70, "PROD1", "V1"));
        Assert.Equal("/Default.aspx?ID=70&ProductID=A%26B", PdpPreview.BuildUrl(70, "A&B"));
    }
}
