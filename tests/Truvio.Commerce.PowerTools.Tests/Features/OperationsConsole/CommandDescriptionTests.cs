using Xunit;
using Truvio.Commerce.PowerTools.Features.OperationsConsole.Dw;

namespace Truvio.Commerce.PowerTools.Tests.Features.OperationsConsole;

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
