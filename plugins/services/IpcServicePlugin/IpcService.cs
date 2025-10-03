using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace IpcServicePlugin;

public class IpcServicePlugin : IServicePlugin
{
    private readonly ILogger<IpcServicePlugin> _logger;

    public IServiceProvider Services { get; private set; } = default!;

    public IpcServicePlugin(ILogger<IpcServicePlugin> logger)
    {
        _logger = logger;
    }

    public string Name => "ipc";

    public void OnLoaded(IServiceProvider services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public object GetService() => new IpcClient(_logger);
}

public class IpcClient
{
    private readonly ILogger _logger;

    public IpcClient(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<string> SendAsync(string pipeName, string message, CancellationToken cancellationToken = default)
    {
        using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(cancellationToken);
        using var writer = new StreamWriter(client) { AutoFlush = true };
        using var reader = new StreamReader(client);
        await writer.WriteLineAsync(message);
        var response = await reader.ReadLineAsync();
        _logger.LogInformation("Sent IPC message to {Pipe}", pipeName);
        return response ?? string.Empty;
    }
}


