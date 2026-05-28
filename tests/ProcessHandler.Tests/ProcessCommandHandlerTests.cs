using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using ProcessHandler;
using ProcessServicePlugin;
using TaskHub.Abstractions;
using Xunit;

namespace ProcessHandler.Tests;

public class ProcessCommandHandlerTests
{
    [Fact]
    public void CommandsIncludeProcessStartAliases()
    {
        var handler = new StartProcessCommandHandler();

        Assert.Contains("process-start", handler.Commands);
        Assert.Contains("start-process", handler.Commands);
        Assert.Equal("process", handler.ServiceName);
    }

    [Fact]
    public void CreateParsesStartProcessRequest()
    {
        var handler = new StartProcessCommandHandler();
        var payload = JsonSerializer.SerializeToElement(new
        {
            fileName = "dotnet",
            argumentList = new[] { "--version" },
            timeoutMilliseconds = 1000
        });

        var command = Assert.IsType<StartProcessCommand>(((ICommandHandler<StartProcessCommand>)handler).Create(payload, NullLogger.Instance));

        Assert.Equal("dotnet", command.Request.FileName);
        Assert.Equal(new[] { "--version" }, command.Request.ArgumentList);
        Assert.Equal(1000, command.Request.TimeoutMilliseconds);
    }

    [Fact]
    public async Task CommandExecutesProcessService()
    {
        var shell = ShellCommand("printf 'handler-out'; printf 'handler-err' >&2", "echo handler-out & echo handler-err 1>&2");
        var command = new StartProcessCommand(new StartProcessRequest
        {
            FileName = shell.FileName,
            ArgumentList = shell.Arguments,
            TimeoutMilliseconds = 5000
        }, NullLogger.Instance);
        var service = new ProcessServicePlugin.ProcessServicePlugin();

        OperationResult result = await command.ExecuteAsync(service, CancellationToken.None);

        Assert.Equal("success", result.Result);
        Assert.Equal("handler-out", result.Payload?.GetProperty("stdout").GetString()?.Trim());
        Assert.Equal("handler-err", result.Payload?.GetProperty("stderr").GetString()?.Trim());
    }

    private static (string FileName, string[] Arguments) ShellCommand(string unixCommand, string windowsCommand)
    {
        return OperatingSystem.IsWindows()
            ? ("cmd.exe", new[] { "/c", windowsCommand })
            : ("/bin/sh", new[] { "-c", unixCommand });
    }
}
