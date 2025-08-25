using MonitorHandler;
using Xunit;

namespace MonitorHandler.Tests;

public class MonitorCommandHandlerTests
{
    [Fact]
    public void CommandsIncludeMonitorInfo()
    {
        var handler = new MonitorCommandHandler();
        Assert.Contains("monitor-info", handler.Commands);
        Assert.Equal("monitor", handler.ServiceName);
    }
}

