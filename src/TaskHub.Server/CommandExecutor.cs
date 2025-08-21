using System;
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

    public async Task Execute(string handlerName, string arguments, CancellationToken token)
    {
        var handler = _manager.GetHandler(handlerName);
        if (handler == null)
        {
            throw new InvalidOperationException($"Handler {handlerName} not found.");
        }

        var service = _manager.GetService(handler.ServiceName);
        await handler.ExecuteAsync(arguments, service, token);
    }
}
