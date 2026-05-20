using CleanTempHandler;
using System.Text.Json;
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

    [Fact]
    public void CreateUsesCommandNameForDispatch()
    {
        var handler = new CleanTempCommandHandler();
        var payload = JsonSerializer.SerializeToElement(new { path = "/tmp/taskhub" });

        var command = handler.Create("delete-folder", payload);

        Assert.IsType<DeleteFolderCommand>(command);
    }
}
