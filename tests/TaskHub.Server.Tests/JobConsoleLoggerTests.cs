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
}
