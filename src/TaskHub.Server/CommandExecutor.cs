using System;
using System.Collections.Generic;
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

    private async Task<JsonElement> ExecuteInternal(string command, JsonElement payload, CancellationToken token)
    {
        var handler = _manager.GetHandler(command);
        if (handler == null)
        {
            throw new InvalidOperationException($"Handler {command} not found.");
        }

        var service = _manager.GetService(handler.ServiceName);
        var cmd = handler.Create(payload);
        return await cmd.ExecuteAsync(service, token);
    }

    public async Task Execute(string command, JsonElement payload, CancellationToken token)
    {
        _ = await ExecuteInternal(command, payload, token);
    }

    public async Task<JsonElement> ExecuteChain(IEnumerable<string> commands, JsonElement payload, CancellationToken token)
    {
        var current = payload;
        foreach (var command in commands)
        {
            current = await ExecuteInternal(command, current, token);
        }

        return current;
    }
}
