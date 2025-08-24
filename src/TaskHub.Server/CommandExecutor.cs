using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using TaskHub.Abstractions;
using Hangfire.Server;

namespace TaskHub.Server;

    public class CommandExecutor
    {
        private readonly PluginManager _manager;

        private static readonly ConcurrentDictionary<string, List<ExecutedCommandResult>> _history = new();

        public CommandExecutor(PluginManager manager)
        {
            _manager = manager;
        }

        public static IReadOnlyList<ExecutedCommandResult>? GetHistory(string jobId)
        {
            return _history.TryGetValue(jobId, out var list) ? list : null;
        }

    private async Task<OperationResult> ExecuteInternal(string command, JsonElement payload, ClientWebSocket? socket, CancellationToken token)
    {
        var handler = _manager.GetHandler(command);
        if (handler == null)
        {
            return new OperationResult(null, $"Handler {command} not found.");
        }

        var service = _manager.GetService(handler.ServiceName);
        var cmd = handler.Create(payload);
        return await cmd.ExecuteAsync(service, token, socket);
    }

    public async Task<OperationResult> Execute(string command, JsonElement payload, CancellationToken token, ClientWebSocket? socket = null)
    {
        return await ExecuteInternal(command, payload, socket, token);
    }

    public async Task<OperationResult> ExecuteChain(IEnumerable<string> commands, JsonElement payload, PerformContext context, CancellationToken token, ClientWebSocket? socket = null)
    {
        var current = payload;
        var results = new List<ExecutedCommandResult>();
        OperationResult lastResult = new OperationResult(null, "success");
        foreach (var command in commands)
        {
            var ranAt = DateTimeOffset.UtcNow;
            lastResult = await ExecuteInternal(command, current, socket, token);
            var output = lastResult.Payload ?? JsonDocument.Parse("null").RootElement;
            results.Add(new ExecutedCommandResult(command, ranAt, output));
            current = output;
        }

        _history[context.BackgroundJob.Id] = results;

        return lastResult;
    }
}
