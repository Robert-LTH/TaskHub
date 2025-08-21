using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace TaskHub.Server;

public class CommandExecutor
{
    private readonly PluginManager _manager;

    public CommandExecutor(PluginManager manager)
    {
        _manager = manager;
    }

    public async Task Execute(string command, JsonElement payload, CancellationToken token)
    {
        var handler = _manager.GetHandler(command);
        if (handler == null)
        {
            throw new InvalidOperationException($"Handler {command} not found.");
        }

        var service = _manager.GetService(handler.ServiceName);
        await handler.ExecuteAsync(payload, service, token);
    }
}
