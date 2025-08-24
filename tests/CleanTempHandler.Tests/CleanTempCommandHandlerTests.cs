using CleanTempHandler;
using Xunit;

namespace CleanTempHandler.Tests;

public class CleanTempCommandHandlerTests
{
    [Fact]
    public void CommandsIncludeExpectedValues()
    {
        var handler = new CleanTempCommandHandler();
        Assert.Contains("clean-temp", handler.Commands);
        Assert.Contains("delete-folder", handler.Commands);
        Assert.Equal("filesystem", handler.ServiceName);
    }
}
