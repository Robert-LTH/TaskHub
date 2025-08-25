using BitLockerHandler;
using Xunit;

namespace BitLockerHandler.Tests;

public class BitLockerCommandHandlerTests
{
    [Fact]
    public void CommandsIncludeRotate()
    {
        var handler = new BitLockerCommandHandler();
        Assert.Contains("bitlocker-rotate", handler.Commands);
        Assert.Equal("bitlocker", handler.ServiceName);
    }
}
