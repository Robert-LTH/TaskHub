using EchoHandler;
using Xunit;

namespace EchoHandler.Tests;

public class EchoCommandHandlerTests
{
    [Fact]
    public void CommandsIncludeEcho()
    {
        var handler = new EchoCommandHandler();
        Assert.Contains("echo", handler.Commands);
        Assert.Equal("http", handler.ServiceName);
    }
}
