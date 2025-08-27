using System;
using System.Text.Json;
using TaskHub.Server;
using Xunit;

namespace TaskHub.Server.Tests;

public class CommandChainRequestTests
{
    [Fact]
    public void PropertiesAreSet()
    {
        var payload = JsonDocument.Parse("{}" ).RootElement;
        var request = new CommandChainRequest(new[] { "echo" }, payload, Delay: TimeSpan.FromMinutes(1), CallbackConnectionId: "client1", RequestedBy: "user1");

        Assert.Equal(TimeSpan.FromMinutes(1), request.Delay);
        Assert.Equal("client1", request.CallbackConnectionId);
        Assert.Equal("user1", request.RequestedBy);
    }
}
