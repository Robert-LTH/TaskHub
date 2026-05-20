using System;
using System.Threading;
using System.Threading.Tasks;
using ProcessServicePlugin;
using TaskHub.Abstractions;
using Xunit;

namespace ProcessServicePlugin.Tests;

public class ProcessServicePluginTests
{
    [Fact]
    public void NameIsProcess()
    {
        var plugin = new ProcessServicePlugin();
        Assert.Equal("process", plugin.Name);
    }

    [Fact]
    public async Task StartAsyncCapturesStdoutAndStderr()
    {
        var shell = ShellCommand("printf 'out'; printf 'err' >&2", "echo out & echo err 1>&2");
        var service = new ProcessServicePlugin.ProcessService(Array.Empty<string>(), 5000, 5000);

        OperationResult result = await service.StartAsync(
            shell.FileName,
            null,
            shell.Arguments,
            null,
            null,
            5000,
            CancellationToken.None);

        Assert.Equal("success", result.Result);
        Assert.Equal(0, result.Payload?.GetProperty("exitCode").GetInt32());
        Assert.Equal("out", result.Payload?.GetProperty("stdout").GetString()?.Trim());
        Assert.Equal("err", result.Payload?.GetProperty("stderr").GetString()?.Trim());
    }

    [Fact]
    public async Task StartAsyncReturnsStdErrForNonZeroExit()
    {
        var shell = ShellCommand("printf 'bad' >&2; exit 7", "echo bad 1>&2 & exit /b 7");
        var service = new ProcessServicePlugin.ProcessService(Array.Empty<string>(), 5000, 5000);

        OperationResult result = await service.StartAsync(
            shell.FileName,
            null,
            shell.Arguments,
            null,
            null,
            5000,
            CancellationToken.None);

        Assert.Equal("Process exited with code 7", result.Result);
        Assert.Equal(7, result.Payload?.GetProperty("exitCode").GetInt32());
        Assert.Equal("bad", result.Payload?.GetProperty("stderr").GetString()?.Trim());
    }

    [Fact]
    public async Task StartAsyncHonorsAllowedExecutables()
    {
        var shell = ShellCommand("printf 'out'", "echo out");
        var service = new ProcessServicePlugin.ProcessService(new[] { "definitely-not-this" }, 5000, 5000);

        OperationResult result = await service.StartAsync(
            shell.FileName,
            null,
            shell.Arguments,
            null,
            null,
            5000,
            CancellationToken.None);

        Assert.Null(result.Payload);
        Assert.Contains("not allowed", result.Result);
    }

    private static (string FileName, string[] Arguments) ShellCommand(string unixCommand, string windowsCommand)
    {
        return OperatingSystem.IsWindows()
            ? ("cmd.exe", new[] { "/c", windowsCommand })
            : ("/bin/sh", new[] { "-c", unixCommand });
    }
}
