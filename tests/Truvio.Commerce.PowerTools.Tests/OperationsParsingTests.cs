using Truvio.Commerce.PowerTools.Core.Operations.Dw;
using Xunit;

namespace Truvio.Commerce.PowerTools.Tests;

/// <summary>
/// The two string formats the Operations tools have to understand: DW's add-in parameter XML,
/// and the command class names the admin command log stores.
/// </summary>
public class AddInSettingsParsingTests
{
    private const string RealSettings =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <Parameters addin="Dynamicweb.Scheduling.ScheduledTaskAddIns.MethodScheduledTaskAddIn">
          <Parameter addin="Dynamicweb.Scheduling.ScheduledTaskAddIns.MethodScheduledTaskAddIn" name="Assembly" value="Dynamicweb.Core" />
          <Parameter addin="Dynamicweb.Scheduling.ScheduledTaskAddIns.MethodScheduledTaskAddIn" name="Class" value="Dynamicweb.Indexing.Repositories.Tasks.TaskHandler" />
        </Parameters>
        """;

    [Fact]
    public void ParseParameters_ReadsNameValuePairs()
    {
        var parameters = DwTaskReader.ParseParameters(RealSettings);

        Assert.Equal(2, parameters.Count);
        Assert.Equal("Assembly", parameters[0].Name);
        Assert.Equal("Dynamicweb.Core", parameters[0].Value);
    }

    [Fact]
    public void ParseParameters_FallsBackToElementContent()
    {
        var parameters = DwTaskReader.ParseParameters(
            "<Parameters><Parameter name=\"Activity\">Nightly\\Import Customers</Parameter></Parameters>");

        var parameter = Assert.Single(parameters);
        Assert.Equal("Nightly\\Import Customers", parameter.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not xml at all")]
    public void ParseParameters_TreatsUnreadableSettingsAsEmpty(string? settings) =>
        Assert.Empty(DwTaskReader.ParseParameters(settings));
}

public class CommandDescriptionTests
{
    [Theory]
    [InlineData("Dynamicweb.Products.UI.Commands.ProductSaveCommand", "Product save")]
    [InlineData("Dynamicweb.Application.UI.Commands.Repositories.BuildIndexCommand", "Build index")]
    [InlineData("Dynamicweb.Application.UI.Commands.Dashboard.DashboardWidgetDeleteCommand", "Dashboard widget delete")]
    [InlineData("SaveCommand", "Save")]
    [InlineData("", "Unknown command")]
    public void DescribeCommand_TurnsAClassNameIntoAPhrase(string commandType, string expected) =>
        Assert.Equal(expected, DwChangeReader.DescribeCommand(commandType));
}
