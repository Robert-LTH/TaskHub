using DiskSpaceHandler;
using Xunit;

namespace DiskSpaceHandler.Tests;

public class DiskSpaceCommandHandlerTests
{
    [Fact]
    public void CommandsIncludeDiskFree()
    {
        var handler = new DiskSpaceCommandHandler();
        Assert.Contains("disk-free", handler.Commands);
        Assert.Equal("filesystem", handler.ServiceName);
    }
}

