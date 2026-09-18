using Xunit;
using static Truvio.Commerce.PowerTools.Tests.Features.OperationsConsole.OperationsTestData;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Core;

namespace Truvio.Commerce.PowerTools.Tests.Features.OperationsConsole;

public class ActivityLinkTests
{
    [Theory]
    [InlineData("Nightly\\Import", "Nightly\\Import")]
    [InlineData("Nightly/Import", "Nightly\\Import")]
    [InlineData("  \\Import\\  ", "Import")]
    [InlineData(null, "")]
    public void Normalise_UnifiesSeparatorsAndTrims(string? input, string expected) =>
        Assert.Equal(expected, ActivityLinks.Normalise(input));

    [Fact]
    public void TasksFor_FindsEveryTaskRunningTheActivity()
    {
        var activity = Activity("Import Customers", group: "Nightly");
        var tasks = new[]
        {
            Task(id: 1, linkedActivityId: "Nightly\\Import Customers"),
            Task(id: 2, linkedActivityId: "nightly/import customers"),
            Task(id: 3, linkedActivityId: "Other")
        };

        var linked = ActivityLinks.TasksFor(tasks, activity);

        Assert.Equal([1, 2], linked.Select(t => t.Id).Order());
    }
}
