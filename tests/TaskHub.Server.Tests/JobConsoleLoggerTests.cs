using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TaskHub.Server;
using TaskHub.Abstractions;
using Xunit;

namespace TaskHub.Server.Tests;

public class JobConsoleLoggerTests
{
    [Fact]
    public void Log_AppendsToStore()
    {
        var store = new JobLogStore();
        var logger = new JobConsoleLogger(NullLogger.Instance, "cmd", "job1", store, Array.Empty<ILogPublisher>(), _ => null);
        logger.LogInformation("hello");
        var logs = store.GetLogs("job1");
        Assert.Single(logs!);
        Assert.Equal("cmd hello", logs![0]);
    }

    [Fact]
    public void Append_DoesNotEvictSameJobForManyMessages()
    {
        var store = new JobLogStore();

        for (var i = 0; i < 150; i++)
        {
            store.Append("job1", $"message-{i}");
        }

        var logs = store.GetLogs("job1");
        Assert.NotNull(logs);
        Assert.Equal(150, logs!.Count);
    }
}
