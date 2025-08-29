using System;
using System.Text.Json;
using TaskHub.Server;
using Xunit;

namespace TaskHub.Server.Tests;

public class RecurringCommandChainRequestTests
{
    private static readonly string[] Commands = new[] { "echo" };

    [Fact]
    public void PropertiesAreSet()
    {
        var payload = JsonDocument.Parse("{}").RootElement;
        var request = new RecurringCommandChainRequest(Commands, payload, "* * * * *", TimeSpan.FromMinutes(1), CallbackConnectionId: "client1");

        Assert.Equal(new[] { "echo" }, request.Commands);
        Assert.Equal("* * * * *", request.CronExpression);
        Assert.Equal(TimeSpan.FromMinutes(1), request.Delay);
        Assert.Null(request.Signature);
        Assert.Equal("client1", request.CallbackConnectionId);
    }
}

