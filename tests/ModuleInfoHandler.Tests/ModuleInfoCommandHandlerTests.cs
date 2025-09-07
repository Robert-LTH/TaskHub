using ModuleInfoHandler;
using Xunit;

namespace ModuleInfoHandler.Tests;

public class ModuleInfoCommandHandlerTests
{
    [Fact]
    public void CommandsIncludeExpectedValues()
    {
        var handler = new ModuleInfoCommandHandler();
        Assert.Contains("loaded-modules", handler.Commands);
        Assert.Equal("appdomain", handler.ServiceName);
    }
}

