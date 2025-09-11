using System;
using System.Text.Json;
using TaskHub.Server;
using Xunit;

namespace TaskHub.Server.Tests;

public class RecurringCommandChainRequestTests
{
    private static readonly CommandItem[] Commands = new[] { new CommandItem("echo", JsonDocument.Parse("{}" ).RootElement) };

    [Fact]
    public void PropertiesAreSet()
    {
        var request = new RecurringCommandChainRequest { Commands = Commands, CronExpression = "* * * * *", Delay = TimeSpan.FromMinutes(1), CallbackConnectionId = "client1" };

        Assert.Equal("echo", Assert.Single(request.Commands).Command);
        Assert.Equal("* * * * *", request.CronExpression);
        Assert.Equal(TimeSpan.FromMinutes(1), request.Delay);
        Assert.Null(request.Signature);
        Assert.Equal("client1", request.CallbackConnectionId);
    }
}

