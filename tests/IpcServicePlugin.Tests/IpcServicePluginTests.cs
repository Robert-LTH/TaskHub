using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using IpcServicePlugin;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IpcServicePlugin.Tests;

public class IpcServicePluginTests
{
    [Fact]
    public void NameIsIpc()
    {
        var plugin = new IpcServicePlugin(NullLogger<IpcServicePlugin>.Instance);
        Assert.Equal("ipc", plugin.Name);
    }

    [Fact]
    public async Task CanSendMessage()
    {
        var plugin = new IpcServicePlugin(NullLogger<IpcServicePlugin>.Instance);
        var client = (IpcClient)plugin.GetService();
        var pipeName = Guid.NewGuid().ToString();

        using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

        var serverTask = Task.Run(async () =>
        {
            await server.WaitForConnectionAsync();
            using var reader = new StreamReader(server);
            using var writer = new StreamWriter(server) { AutoFlush = true };
            var message = await reader.ReadLineAsync();
            await writer.WriteLineAsync(message?.ToUpperInvariant());
        });

        var response = await client.SendAsync(pipeName, "hello", CancellationToken.None);
        Assert.Equal("HELLO", response);
        await serverTask;
    }
}
