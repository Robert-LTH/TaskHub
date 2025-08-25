using CcmExecHandler;
using Xunit;

namespace CcmExecHandler.Tests;

public class CcmExecCommandHandlerTests
{
    [Fact]
    public void CommandsIncludeCcmExwc()
    {
        var handler = new CcmExecCommandHandler();
        Assert.Contains("ccmexwc", handler.Commands);
        Assert.Equal("configurationmanager", handler.ServiceName);
    }
}
