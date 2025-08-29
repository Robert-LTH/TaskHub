using System;
using System.Text.Json;
using TaskHub.Server;
using Xunit;

namespace TaskHub.Server.Tests;

public class CommandChainRequestTests
{
    private static readonly string[] Commands = new[] { "echo" };

    [Fact]
    public void PropertiesAreSet()
    {
        var payload = JsonDocument.Parse("{}" ).RootElement;
        var request = new CommandChainRequest(Commands, payload, Delay: TimeSpan.FromMinutes(1), CallbackConnectionId: "client1", RequestedBy: "user1");

        Assert.Equal(TimeSpan.FromMinutes(1), request.Delay);
        Assert.Equal("client1", request.CallbackConnectionId);
        Assert.Equal("user1", request.RequestedBy);
    }
}
