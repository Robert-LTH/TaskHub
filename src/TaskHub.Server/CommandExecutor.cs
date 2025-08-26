using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;
using Hangfire.Server;

namespace TaskHub.Server;

public class CommandExecutor
{
    private readonly PluginManager _manager;

    private const int MaxHistoryEntries = 100;
    private static readonly ConcurrentDictionary<string, List<ExecutedCommandResult>> _history = new();
    private static readonly ConcurrentQueue<string> _historyOrder = new();
    private static readonly JsonElement NullElement = JsonDocument.Parse("null").RootElement;

    public CommandExecutor(PluginManager manager)
    {
        _manager = manager;
    }

    public static IReadOnlyList<ExecutedCommandResult>? GetHistory(string jobId)
    {
        return _history.TryRemove(jobId, out var list) ? list : null;
    }

    private async Task<OperationResult> ExecuteInternal(string command, JsonElement payload, CancellationToken token)
    {
        var handler = _manager.GetHandler(command);
        if (handler == null)
        {
            return new OperationResult(null, $"Handler {command} not found.");
        }

        var service = _manager.GetService(handler.ServiceName);
        return await handler.ExecuteAsync(payload, service, token);
    }

    public async Task<OperationResult> Execute(string command, JsonElement payload, CancellationToken token)
    {
        return await ExecuteInternal(command, payload, token);
    }

    public async Task<OperationResult> ExecuteChain(IEnumerable<string> commands, JsonElement payload, PerformContext context, CancellationToken token)
    {
        var current = payload;
        var results = new List<ExecutedCommandResult>();
        OperationResult lastResult = new OperationResult(null, "success");
        foreach (var command in commands)
        {
            var ranAt = DateTimeOffset.UtcNow;
            lastResult = await ExecuteInternal(command, current, token);
            var output = lastResult.Payload ?? NullElement;
            results.Add(new ExecutedCommandResult(command, ranAt, output));
            current = output;
        }

        _history[context.BackgroundJob.Id] = results;
        _historyOrder.Enqueue(context.BackgroundJob.Id);
        while (_historyOrder.Count > MaxHistoryEntries && _historyOrder.TryDequeue(out var oldId))
        {
            _history.TryRemove(oldId, out _);
        }

        return lastResult;
    }
}
