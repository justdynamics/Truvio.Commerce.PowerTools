using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.OperationsConsole.OperationsTestData;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Core;

namespace Truvio.Commerce.PowerTools.Tests.Features.OperationsConsole;

public class OpsFormatTests
{
    [Theory]
    [InlineData("Dynamicweb.DataIntegration.Integration.JobScheduledTaskAddIn, Dynamicweb.DataIntegration", "JobScheduledTaskAddIn")]
    [InlineData("Dynamicweb.Scheduling.ScheduledTaskAddIns.MethodScheduledTaskAddIn", "MethodScheduledTaskAddIn")]
    [InlineData("BareType", "BareType")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void ShortTypeName_StripsNamespaceAndAssembly(string? input, string expected) =>
        Assert.Equal(expected, OpsFormat.ShortTypeName(input));

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    public void Bytes_UsesBinaryUnits(long input, string expected) =>
        Assert.Equal(expected, OpsFormat.Bytes(input));

    [Fact]
    public void Relative_HandlesPastPresentFutureAndNever()
    {
        Assert.Equal("never", OpsFormat.Relative(null, Now));
        Assert.Equal("just now", OpsFormat.Relative(Now.AddSeconds(-5), Now));
        Assert.Equal("5 min ago", OpsFormat.Relative(Now.AddMinutes(-5), Now));
        Assert.Equal("3 h ago", OpsFormat.Relative(Now.AddHours(-3), Now));
        Assert.Equal("2 d ago", OpsFormat.Relative(Now.AddDays(-2), Now));
        Assert.Equal("in 4 h", OpsFormat.Relative(Now.AddHours(4), Now));
    }

    [Fact]
    public void Duration_ScalesFromSubSecondToHours()
    {
        Assert.Equal("-", OpsFormat.Duration(null));
        Assert.Equal("0.4 s", OpsFormat.Duration(TimeSpan.FromMilliseconds(400)));
        Assert.Equal("12 s", OpsFormat.Duration(TimeSpan.FromSeconds(12)));
        Assert.Equal("3 m 05 s", OpsFormat.Duration(TimeSpan.FromSeconds(185)));
        Assert.Equal("1 h 12 m", OpsFormat.Duration(TimeSpan.FromMinutes(72)));
    }

    [Theory]
    [InlineData(0, "once")]
    [InlineData(5, "every 5 min")]
    [InlineData(60, "every 1 h")]
    [InlineData(90, "every 1 h 30 min")]
    [InlineData(1440, "every 1 d")]
    [InlineData(2880, "every 2 d")]
    public void Interval_ReadsAsAPhrase(int minutes, string expected) =>
        Assert.Equal(expected, OpsFormat.Interval(minutes));
}
