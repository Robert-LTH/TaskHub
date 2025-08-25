using IntuneManagementExtensionHandler;
using Xunit;

namespace IntuneManagementExtensionHandler.Tests;

public class IntuneManagementExtensionCommandHandlerTests
{
    [Fact]
    public void CommandsIncludeIntuneSync()
    {
        var handler = new IntuneManagementExtensionCommandHandler();
        Assert.Contains("intune-sync", handler.Commands);
        Assert.Equal("powershell", handler.ServiceName);
    }
}
