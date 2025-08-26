using System;
using System.Text.Json;
using TaskHub.Server;
using Xunit;

namespace TaskHub.Server.Tests;

public class RecurringCommandChainRequestTests
{
    [Fact]
    public void PropertiesAreSet()
    {
        var payload = JsonDocument.Parse("{}").RootElement;
        var request = new RecurringCommandChainRequest(new[] { "echo" }, payload, "* * * * *", TimeSpan.FromMinutes(1), callbackConnectionId: "client1");

        Assert.Equal(new[] { "echo" }, request.Commands);
        Assert.Equal("* * * * *", request.CronExpression);
        Assert.Equal(TimeSpan.FromMinutes(1), request.Delay);
        Assert.Null(request.Signature);
        Assert.Equal("client1", request.CallbackConnectionId);
    }
}

