using System;
using System.Text.Json;
using TaskHub.Server;
using Xunit;

namespace TaskHub.Server.Tests;

public class CommandChainRequestTests
{
    private static readonly CommandItem[] Commands = new[] { new CommandItem("echo", JsonDocument.Parse("{}" ).RootElement) };

    [Fact]
    public void PropertiesAreSet()
    {
        var request = new CommandChainRequest { Commands = Commands, Delay = TimeSpan.FromMinutes(1), CallbackConnectionId = "client1", RequestedBy = "user1" };

        Assert.Equal(TimeSpan.FromMinutes(1), request.Delay);
        Assert.Equal("client1", request.CallbackConnectionId);
        Assert.Equal("user1", request.RequestedBy);
    }
}
